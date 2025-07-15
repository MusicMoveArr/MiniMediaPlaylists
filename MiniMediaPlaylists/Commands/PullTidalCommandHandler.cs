using System.Net;
using System.Text;
using MiniMediaPlaylists.Models.Tidal;
using MiniMediaPlaylists.Repositories;
using MiniMediaPlaylists.Services;
using Spectre.Console;

namespace MiniMediaPlaylists.Commands;

public class PullTidalCommandHandler
{
    private readonly TidalRepository _tidalRepository;

    public PullTidalCommandHandler(string connectionString)
    {
        _tidalRepository = new TidalRepository(connectionString);
    }

    public async Task PullTidalPlaylists(
        string tidalClientId, 
        string tidalSecretId, 
        string tidalCountryCode,
        string authRedirectUri,
        string authCallbackListener,
        string likedSongsPlaylistName,
        string ownerName)
    {
        TidalAPIService tidalApiService = new TidalAPIService(tidalClientId, tidalSecretId, tidalCountryCode);

        var owner = await _tidalRepository.GetOwnerByNameAsync(ownerName);
        if (owner == null)
        {
            if (!await HandleTidalAuthAsync(tidalApiService, authRedirectUri, authCallbackListener, ownerName, tidalClientId, tidalSecretId))
            {
                Console.WriteLine("Authentication failed...");
                return;
            }
        }
        
        await tidalApiService.AuthenticateWithRefreshTokenAsync(owner.AuthRefreshToken);
        var currentUser = await tidalApiService.GetCurrentUserAsync();
        var playlists = await tidalApiService.GetPlaylistsAsync(currentUser.Data.Id);
        playlists = await GetAllPlaylistsAsync(playlists, tidalApiService);

        await AnsiConsole.Progress()
            .HideCompleted(true)
            .AutoClear(true)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn()
                {
                    Alignment = Justify.Left
                },
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
            })
            .StartAsync(async ctx =>
            {
                var totalProgressTask = ctx.AddTask(Markup.Escape($"Processing Playlists 0 of {playlists.Data.Count} processed"));
                totalProgressTask.MaxValue = playlists.Data.Count;
                
                foreach (var playlist in playlists.Data)
                {
                    try
                    {
                        await _tidalRepository.UpsertPlaylistAsync(
                            playlist.Id,
                            owner.Id,
                            $"https://listen.tidal.com/playlist/{playlist.Id}",
                            playlist.Attributes.Name,
                            playlist.Attributes.Description,
                            playlist.Attributes.Bounded,
                            string.IsNullOrWhiteSpace(playlist.Attributes.Duration) ? string.Empty : playlist.Attributes.Duration,
                            playlist.Attributes.NumberOfItems,
                            playlist.Attributes.CreatedAt,
                            playlist.Attributes.LastModifiedAt,
                            playlist.Attributes.Privacy,
                            playlist.Attributes.AccessType,
                            playlist.Attributes.PlaylistType);

                        var playlistInfo = await tidalApiService.GetPlaylistByIdAsync(playlist.Id);
                        playlistInfo = await GetAllTracksAsync(playlistInfo, tidalApiService);
                        var tracks = playlistInfo.Included.Where(t => t.Type == "tracks").ToList();
                        
                        var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Attributes.Name}', 0 of {tracks.Count} processed"));
                        task.MaxValue = tracks.Count;
                        
                        foreach (var track in tracks)
                        {
                            var trackInfo = await tidalApiService.GetTrackByIdAsync(track.Id);
                            var album = trackInfo.Included.FirstOrDefault(album => album.Type == "albums");
                            var artists = trackInfo.Included
                                .Where(artist => artist.Type == "artists")
                                .ToList();

                            if (album == null || !artists.Any())
                            {
                                continue;
                            }

                            var metaData =  playlistInfo.Data.Relationships.Items.Data
                                .FirstOrDefault(x => x.Id == track.Id);

                            await _tidalRepository.UpsertPlaylistTrackAsync(track.Id,
                                playlist.Id,
                                owner.Id,
                                metaData?.Meta?.ItemId ?? string.Empty,
                                track.Attributes.Title,
                                track.Attributes.ISRC,
                                track.Attributes.Explicit,
                                album.Attributes.Title,
                                album.Attributes.ReleaseDate,
                                album.Attributes.BarcodeId,
                                album.Attributes.Explicit,
                                album.Attributes.Type,
                                artists.FirstOrDefault()?.Attributes.Name,
                                false,
                                metaData?.Meta?.AddedAt ?? DateTime.Now);

                            task.Increment(1);
                            task.Description(Markup.Escape($"Processing Playlist '{playlist.Attributes.Name}', {task.Value} of {tracks.Count} processed"));
                        }
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.WriteLine(Markup.Escape($"Error: {e.Message}"));
                    }
                    
                    totalProgressTask.Value++;
                    totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {playlists.Data.Count} processed"));
                }
            });
        
        await _tidalRepository.SetLastSyncTimeAsync(owner.Id);

    }
    private async Task<bool> HandleTidalAuthAsync(
        TidalAPIService tidalApiService,
        string authRedirectUri,
        string authCallbackListener,
        string ownerName,
        string clientId,
        string secretId)
    {
        string tidalLoginUrl = tidalApiService.GetPkceLoginUrl(authRedirectUri);
        Console.WriteLine("Open the following URL in your browser:\n" + tidalLoginUrl);
        
        // start local listener
        var http = new HttpListener();
        http.Prefixes.Add(authCallbackListener);
        http.Start();
        Console.WriteLine($"Listening on {authCallbackListener}");

        var ctx = await http.GetContextAsync();
        var code = ctx.Request.QueryString["code"];

        // respond to browser
        byte[] responseBytes = Encoding.UTF8.GetBytes("OK - you can close now this window.");
        ctx.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        ctx.Response.OutputStream.Close();
        http.Stop();

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }
        
        await tidalApiService.AuthenticateWithCodeAsync(authRedirectUri, code);
        var currentUser = await tidalApiService.GetCurrentUserAsync();

        if (string.IsNullOrWhiteSpace(currentUser?.Data?.Attributes?.Username))
        {
            Console.WriteLine("No username received back from Tidal...");
            return false;
        }
        await _tidalRepository.UpsertOwnerAsync(currentUser.Data.Attributes.Username, clientId, secretId,
            tidalApiService.AuthenticationResponse.RefreshToken);

        return true;
    }

    private async Task<PlaylistByIdResponse?> GetAllTracksAsync(PlaylistByIdResponse playlistResponse, TidalAPIService tidalApiService)
    {
        if (playlistResponse?.Included?.Count >= 20)
        {
            string? nextPage = playlistResponse.Data.Relationships?.Items?.Links?.Next;
            while (!string.IsNullOrWhiteSpace(nextPage))
            {
                var tempTracks = await tidalApiService.GetPlaylistByIdNextAsync(nextPage);

                if (tempTracks?.Included?.Count > 0)
                {
                    playlistResponse.Included.AddRange(tempTracks.Included);
                }

               if (tempTracks?.Data?.Count > 0)
               {
                   playlistResponse.Data
                       ?.Relationships
                       ?.Items
                       ?.Data
                       ?.AddRange(tempTracks.Data);
               }
               nextPage = tempTracks?.Links?.Next;
            }
        }
        
        return playlistResponse;
    }
    private async Task<PlaylistResponse?> GetAllPlaylistsAsync(PlaylistResponse playlistResponse, TidalAPIService tidalApiService)
    {
        if (playlistResponse?.Data?.Count >= 20)
        {
            string? nextPage = playlistResponse?.Links?.Next;
            while (!string.IsNullOrWhiteSpace(nextPage))
            {
                var tempPlaylists = await tidalApiService.GetPlaylistsNextAsync(nextPage);

                if (tempPlaylists?.Data?.Count > 0)
                {
                    playlistResponse.Data.AddRange(tempPlaylists.Data);
                }
                
                nextPage = tempPlaylists?.Links?.Next;
            }
        }
        
        return playlistResponse;
    }
    
}