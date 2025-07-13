using System.Net;
using System.Security.Cryptography;
using System.Text;
using MiniMediaPlaylists.Repositories;
using Spectre.Console;
using SpotifyAPI.Web;

namespace MiniMediaPlaylists.Commands;

public class PullSpotifyCommandHandler
{
    private readonly SpotifyRepository _spotifyRepository;
    public PullSpotifyCommandHandler(string connectionString)
    {
        _spotifyRepository = new SpotifyRepository(connectionString);
    }

    public async Task PullSpotifyPlaylists(
        string spotifyClientId, 
        string spotifySecretId, 
        string authRedirectUri, 
        string authCallbackListener,
        string ownerName,
        string likedSongsPlaylistName)
    {
        var spotifyOwnerModel = await _spotifyRepository.GetOwnerByNameAsync(ownerName);
        SpotifyClient spotifyClient;
        string refreshToken = string.Empty;

        if (string.IsNullOrWhiteSpace(spotifyOwnerModel?.AuthRefreshToken))
        {
            var authToken = await HandleSpotifyAuthAsync(authRedirectUri, spotifyClientId, authCallbackListener, spotifySecretId);
            spotifyClient = new SpotifyClient(authToken.AccessToken);
            refreshToken = authToken.RefreshToken;
        }
        else
        {
            var refreshRequest = new AuthorizationCodeRefreshRequest(spotifyOwnerModel.AuthClientId,
                spotifyOwnerModel.AuthSecretId, spotifyOwnerModel.AuthRefreshToken);
            var newToken = await new OAuthClient().RequestToken(refreshRequest);
            spotifyClient = new SpotifyClient(newToken.AccessToken);
            refreshToken = refreshRequest.RefreshToken;
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            Console.WriteLine("Received no refresh token back from spotify??");
            return;
        }

        var currentUser = await spotifyClient.UserProfile.Current();
        var playlists = await spotifyClient.Playlists.CurrentUsers();
        var ownerId = await _spotifyRepository.UpsertOwnerAsync(currentUser.Id, spotifyClientId, spotifySecretId, refreshToken);

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
                var allPlaylists = new List<FullPlaylist>();
                await foreach (var playlist in spotifyClient.Paginate(playlists))
                {
                    allPlaylists.Add(playlist);
                }

                var totalProgressTask = ctx.AddTask(Markup.Escape($"Processing Playlists 0 of {allPlaylists.Count} processed"));
                totalProgressTask.MaxValue = allPlaylists.Count;

                if (string.IsNullOrWhiteSpace(likedSongsPlaylistName))
                {
                    var savedTracks = new List<SavedTrack>();
                    await foreach (var savedTrack in spotifyClient.Paginate(await spotifyClient.Library.GetTracks()))
                    {
                        savedTracks.Add(savedTrack);
                    }
                    
                    string uniqueHashId =
                        BitConverter.ToString(SHA256.Create()
                                .ComputeHash(Encoding.UTF8.GetBytes(likedSongsPlaylistName)))
                            .Replace("-", string.Empty);
                    
                    allPlaylists.Add(new FullPlaylist
                    {
                        Id = uniqueHashId,
                        Href = string.Empty,
                        Name = likedSongsPlaylistName,
                        //Tracks = savedTracks
                        //Owner = currentUser
                    });
                }
                
                foreach (var playlist in allPlaylists)
                {
                    try
                    {
                        await _spotifyRepository.UpsertPlaylistAsync(playlist.Id,
                            ownerId,
                            playlist.Href,
                            playlist.Name,
                            playlist.SnapshotId,
                            playlist.Tracks.Total ?? 0,
                            playlist.Uri,
                            DateTime.Now,
                            DateTime.Now);
                
                        var tracks = await spotifyClient.Playlists.GetItems(playlist.Id);

                        var allTracks = new List<PlaylistTrack<IPlayableItem>>();
                        await foreach (var item in spotifyClient.Paginate(tracks))
                        {
                            allTracks.Add(item);
                        }
                        var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Name}', 0 of {allTracks.Count} processed"));
                        task.MaxValue = allTracks.Count;

                        foreach (var item in allTracks)
                        {
                            if (item.Track is FullTrack track)
                            {
                                int artistIndex = 0;
                                foreach (var artist in track.Artists)
                                {
                                    await _spotifyRepository.UpsertPlaylistTrackArtistAsync(track.Id, 
                                        artist.Id, 
                                        track.Album.Id, 
                                        artist.Name, 
                                        artistIndex++);
                                }
                        
                                await _spotifyRepository.UpsertPlaylistTrackAsync(track.Id,
                                    playlist.Id,
                                    ownerId,
                                    track.Album.AlbumType,
                                    track.Album.Id,
                                    track.Album.Name,
                                    track.Album.ReleaseDate,
                                    track.Album.TotalTracks,
                                    track.Artists.FirstOrDefault().Name ?? string.Empty,
                                    track.Name,
                                    item.AddedBy.Id,
                                    item.AddedBy.Type,
                                    false,
                                    item.AddedAt ?? DateTime.Now);
                            }
                            task.Increment(1);
                            task.Description(Markup.Escape($"Processing Playlist '{playlist.Name}', {task.Value} of {allTracks.Count} processed"));
                        }
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.WriteLine(Markup.Escape($"Error: {e.Message}"));
                    }
                    
                    totalProgressTask.Value++;
                    totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {allPlaylists.Count} processed"));
                }
            });

        await _spotifyRepository.SetLastSyncTimeAsync(ownerId);
    }

    private async Task<AuthorizationCodeTokenResponse> HandleSpotifyAuthAsync(
        string authRedirectUri,
        string spotifyClientId,
        string authCallbackListener,
        string spotifySecretId)
    {
        var loginRequest = new LoginRequest(
            new Uri(authRedirectUri),
            spotifyClientId,
            LoginRequest.ResponseType.Code
        )
        {
            Scope = new[]
            {
                Scopes.PlaylistReadPrivate,
                Scopes.PlaylistReadCollaborative,
                Scopes.UserReadPrivate,
                Scopes.PlaylistModifyPublic,
                Scopes.PlaylistModifyPrivate,
                Scopes.UserLibraryModify,
                Scopes.UserLibraryRead
            }
        };
        var uri = loginRequest.ToUri();
        Console.WriteLine("Open the following URL in your browser:\n" + uri);

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

        var tokenResponse = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(spotifyClientId, spotifySecretId, code, new Uri(authRedirectUri))
        );

        return tokenResponse;
    }
}