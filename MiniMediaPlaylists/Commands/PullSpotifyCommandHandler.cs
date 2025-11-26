using System.Net;
using System.Security.Cryptography;
using System.Text;
using DapperBulkQueries.Common;
using DapperBulkQueries.Npgsql;
using MiniMediaPlaylists.Models.SpotifyDto;
using MiniMediaPlaylists.Repositories;
using Npgsql;
using Spectre.Console;
using SpotifyAPI.Web;

namespace MiniMediaPlaylists.Commands;

public class PullSpotifyCommandHandler
{
    private const int MinimumBulkInsert = 100;
    private readonly string _connectionString;
    private readonly SpotifyRepository _spotifyRepository;
    private readonly SnapshotRepository _snapshotRepository;
    private readonly List<SpotifyPlaylistDto> _playlistDtos;
    private readonly List<SpotifyPlaylistTrackDto> _trackDtos;
    private readonly List<SpotifyPlaylistTrackArtistDto> _trackArtistDtos;
    
    public PullSpotifyCommandHandler(string connectionString)
    {
        _connectionString = connectionString;
        _spotifyRepository = new SpotifyRepository(connectionString);
        _snapshotRepository = new SnapshotRepository(connectionString);
        _playlistDtos = new List<SpotifyPlaylistDto>();
        _trackDtos = new List<SpotifyPlaylistTrackDto>();
        _trackArtistDtos = new List<SpotifyPlaylistTrackArtistDto>();
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
        Guid snapshotId = await _snapshotRepository.CreateSnapshotAsync(ownerId, "Spotify");

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

                if (!string.IsNullOrWhiteSpace(likedSongsPlaylistName))
                {
                    var favoritedTracks = new List<SavedTrack>();
                    await foreach (var savedTrack in spotifyClient.Paginate(await spotifyClient.Library.GetTracks()))
                    {
                        favoritedTracks.Add(savedTrack);
                    }
                    
                    string uniqueHashId =
                        BitConverter.ToString(SHA256.Create()
                                .ComputeHash(Encoding.UTF8.GetBytes(likedSongsPlaylistName)))
                            .Replace("-", string.Empty);
                    
                    _playlistDtos.Add(new SpotifyPlaylistDto
                    {
                        Id = uniqueHashId,
                        OwnerId = ownerId,
                        Name = likedSongsPlaylistName,
                        Href = string.Empty,
                        Uri = string.Empty,
                        UpdatedAt = DateTime.Now,
                        AddedAt = DateTime.Now,
                        TrackCount = favoritedTracks.Count,
                        SnapshotId = snapshotId
                    });
                    
                    int playlistSortOrder = 1;
                    foreach (var item in favoritedTracks)
                    {
                        if (item.Track is FullTrack track)
                        {
                            int artistIndex = 0;
                            foreach (var artist in track.Artists)
                            {
                                _trackArtistDtos.Add(GetSpotifyTrackArtistDto(artist, track, artistIndex++));
                            }
                            _trackDtos.Add(GetSpotifyTrackDto(track, item, ownerId, snapshotId, playlistSortOrder++, uniqueHashId, currentUser));
                        }

                        await BulkInsertPlaylistsAsync(MinimumBulkInsert);
                        await BulkInsertTracksAsync(MinimumBulkInsert);
                        await BulkInsertTrackArtistsAsync(MinimumBulkInsert);
                    }
                }
                
                foreach (var playlist in allPlaylists)
                {
                    try
                    {
                        _playlistDtos.Add(new SpotifyPlaylistDto
                        {
                            Id = playlist.Id,
                            OwnerId = ownerId,
                            Name = playlist.Name ?? string.Empty,
                            Href = playlist.Href ?? string.Empty,
                            Uri = playlist.Uri ?? string.Empty,
                            AddedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            TrackCount = playlist.Tracks?.Total ?? 0,
                            SnapshotId = snapshotId
                        });
                
                        var tracks = await spotifyClient.Playlists.GetItems(playlist.Id);

                        var allTracks = new List<PlaylistTrack<IPlayableItem>>();
                        await foreach (var item in spotifyClient.Paginate(tracks))
                        {
                            allTracks.Add(item);
                        }
                        var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Name}', 0 of {allTracks.Count} processed"));
                        task.MaxValue = allTracks.Count;

                        int playlistSortOrder = 1;
                        foreach (var item in allTracks)
                        {
                            if (item.Track is FullTrack track)
                            {
                                int artistIndex = 0;
                                foreach (var artist in track.Artists)
                                {
                                    _trackArtistDtos.Add(GetSpotifyTrackArtistDto(artist, track, artistIndex++));
                                }
                                
                                _trackDtos.Add(GetSpotifyTrackDto(track, item, ownerId, snapshotId, playlistSortOrder++, playlist.Id));
                        
                                await BulkInsertPlaylistsAsync(MinimumBulkInsert);
                                await BulkInsertTracksAsync(MinimumBulkInsert);
                                await BulkInsertTrackArtistsAsync(MinimumBulkInsert);
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

        await BulkInsertPlaylistsAsync(0);
        await BulkInsertTracksAsync(0);
        await BulkInsertTrackArtistsAsync(0);
        
        await _spotifyRepository.SetLastSyncTimeAsync(ownerId);
        await _snapshotRepository.SetSnapshotCompleteAsync(snapshotId);
    }
    
    private SpotifyPlaylistTrackDto GetSpotifyTrackDto(
        FullTrack track, 
        PlaylistTrack<IPlayableItem> item,
        Guid ownerId,
        Guid snapshotId,
        int playlistSortOrder,
        string playlistId)
    {
        return new SpotifyPlaylistTrackDto
        {
            Id = track.Id,
            Name = track.Name ?? string.Empty,
            AlbumName = track.Album.Name,
            ArtistName = track.Artists.FirstOrDefault().Name ?? string.Empty,
            AlbumId = track.Album.Id,
            AlbumType = track.Album.AlbumType,
            AlbumReleaseDate = track.Album.ReleaseDate,
            AlbumTotalTracks = track.Album.TotalTracks.ToString(),
            AddedById = item.AddedBy.Id,
            AddedByType = item.AddedBy.Type,
            OwnerId = ownerId,
            AddedAt = item.AddedAt ?? DateTime.Now,
            IsRemoved = false,
            SnapshotId = snapshotId,
            Playlist_SortOrder = playlistSortOrder,
            PlaylistId = playlistId,
        };
    }

    private SpotifyPlaylistTrackDto GetSpotifyTrackDto(
        FullTrack track, 
        SavedTrack item,
        Guid ownerId,
        Guid snapshotId,
        int playlistSortOrder,
        string playlistId,
        PrivateUser currentUser)
    {
        return new SpotifyPlaylistTrackDto
        {
            Id = track.Id,
            Name = track.Name ?? string.Empty,
            AlbumName = track.Album.Name,
            ArtistName = track.Artists.FirstOrDefault().Name ?? string.Empty,
            AlbumId = track.Album.Id,
            AlbumType = track.Album.AlbumType,
            AlbumReleaseDate = track.Album.ReleaseDate,
            AlbumTotalTracks = track.Album.TotalTracks.ToString(),
            AddedById = currentUser.Id,
            AddedByType = currentUser.Type,
            OwnerId = ownerId,
            AddedAt = item.AddedAt,
            IsRemoved = false,
            SnapshotId = snapshotId,
            Playlist_SortOrder = playlistSortOrder,
            PlaylistId = playlistId,
        };
    }

    private SpotifyPlaylistTrackArtistDto GetSpotifyTrackArtistDto(
        SimpleArtist artist, 
        FullTrack track,
        int artistIndex)
    {
        return new SpotifyPlaylistTrackArtistDto
        {
            TrackId = track.Id, 
            ArtistId = artist.Id, 
            ArtistName = artist.Name, 
            AlbumId = track.Album.Id, 
            Index = artistIndex
        };
    }

    private async Task BulkInsertPlaylistsAsync(int minimumRecords)
    {
        if (_playlistDtos.Count > minimumRecords)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteBulkInsertAsync(
                "playlists_spotify_playlist",
                _playlistDtos,
                SpotifyPlaylistDto.PlaylistDtoColumnNames, 
                onConflict: OnConflict.DoNothing);
            _playlistDtos.Clear();
        }
    }

    private async Task BulkInsertTracksAsync(int minimumRecords)
    {
        if (_trackDtos.Count > minimumRecords)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteBulkInsertAsync(
                "playlists_spotify_playlist_track",
                _trackDtos,
                SpotifyPlaylistTrackDto.PlaylistTrackDtoColumnNames, 
                onConflict: OnConflict.DoNothing);
            _trackDtos.Clear();
        }
    }

    private async Task BulkInsertTrackArtistsAsync(int minimumRecords)
    {
        if (_trackArtistDtos.Count > minimumRecords)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteBulkInsertAsync(
                "playlists_spotify_playlist_track_artist",
                _trackArtistDtos,
                SpotifyPlaylistTrackArtistDto.PlaylistTrackArtistDtoColumnNames, 
                onConflict: OnConflict.DoNothing);
            _trackArtistDtos.Clear();
        }
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