using System.Security.Cryptography;
using System.Text;
using MiniMediaPlaylists.Models.SubsonicDto;
using MiniMediaPlaylists.Repositories;
using Npgsql;
using Spectre.Console;
using DapperBulkQueries.Common;
using DapperBulkQueries.Npgsql;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Models.Navidrome;
using MiniMediaPlaylists.Services;

namespace MiniMediaPlaylists.Commands;

public class PullNavidromeCommandHandler
{
    private readonly string _connectionString;
    private readonly SubSonicRepository _subSonicRepository;
    private readonly SnapshotRepository _snapshotRepository;
    private readonly SnapshotRetentionService _snapshotRetentionService;
    private readonly NavidromeApiService _navidromeApiService;

    public PullNavidromeCommandHandler(string connectionString)
    {
        _connectionString = connectionString;
        _subSonicRepository = new SubSonicRepository(connectionString);
        _snapshotRepository = new SnapshotRepository(connectionString);
        _snapshotRetentionService = new SnapshotRetentionService();
        _navidromeApiService = new NavidromeApiService();
    }

    public async Task PullNavidromePlaylists(
        string serverUrl, 
        string username, 
        string password, 
        string likedSongsPlaylistName, 
        RetentionPolicy retentionPolicy)
    {
        await _navidromeApiService.LoginAsync(serverUrl, username, password);
        var playlists = await _navidromeApiService.GetPlaylistsAsync(serverUrl);

        Guid serverId = await _subSonicRepository.UpsertServerAsync(serverUrl);
        Guid snapshotId = await _snapshotRepository.CreateSnapshotAsync(serverId, "Subsonic");

        var allSnapshots = await _snapshotRepository.GetSnapshotsByServerIdAsync(serverId);
        var snapshotIdsToCleanup = _snapshotRetentionService.GetSnapshotsToRemove(allSnapshots, retentionPolicy);
        await _subSonicRepository.DeleteSnapshotsAsync(snapshotIdsToCleanup);
        await _snapshotRepository.DeleteSnapshotsAsync(snapshotIdsToCleanup);
        
        List<SubsonicPlaylistDto> playlistDtos = new List<SubsonicPlaylistDto>();
        List<SubsonicPlaylistTrackDto> trackDtos = new List<SubsonicPlaylistTrackDto>();
        
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
                var totalProgressTask = ctx.AddTask(Markup.Escape($"Processing Playlists 0 of {playlists.Count} processed"));
                totalProgressTask.MaxValue = playlists.Count;

                string genLikedPlaylistName = string.Empty;
                
                if (!string.IsNullOrWhiteSpace(likedSongsPlaylistName))
                {
                    string uniqueHashId =
                        BitConverter.ToString(SHA256.Create()
                            .ComputeHash(Encoding.UTF8.GetBytes(likedSongsPlaylistName)))
                            .Replace("-", string.Empty);
                    
                    genLikedPlaylistName = $"#{uniqueHashId}";
                    playlists.Insert(0, new PlaylistEntity
                    {
                        Duration = 0,
                        Id = genLikedPlaylistName,
                        Name = likedSongsPlaylistName,
                        Public = false,
                        SongCount = 0,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }

                foreach (var playlist in playlists)
                {
                    try
                    {
                        playlistDtos.Add(new SubsonicPlaylistDto
                        {
                            Id = playlist.Id,
                            Name = !string.IsNullOrWhiteSpace(playlist.Name) ? playlist.Name : string.Empty,
                            Owner = !string.IsNullOrWhiteSpace(playlist.OwnerName) ? playlist.OwnerName : string.Empty,
                            Public = playlist.Public,
                            SongCount = playlist.SongCount,
                            ChangedAt = playlist.UpdatedAt,
                            CreatedAt = playlist.CreatedAt,
                            Comment = !string.IsNullOrWhiteSpace(playlist.Comment) ? playlist.Comment : string.Empty,
                            Duration = (int)playlist.Duration,
                            ServerId = serverId,
                            SnapshotId = snapshotId
                        });
                        
                        List<TrackEntity> tracks = new List<TrackEntity>();
                        if (!string.IsNullOrWhiteSpace(genLikedPlaylistName) &&
                            string.Equals(genLikedPlaylistName, playlist.Id))
                        {
                            tracks = await _navidromeApiService.GetStarredTracksAsync(serverUrl);
                        }
                        else
                        {
                            tracks = await _navidromeApiService.GetPlaylistTracksAsync(serverUrl, playlist.Id);
                        }
                        
                        var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Name}', 0 of {tracks.Count} processed"));
                        task.MaxValue = tracks.Count;
                        int playlistSortOrder = 1;
                        
                        trackDtos.AddRange(tracks.Select(track => new SubsonicPlaylistTrackDto
                        {
                            Id = !string.IsNullOrWhiteSpace(track.MediaFileId) ? track.MediaFileId : track.Id,
                            Title = !string.IsNullOrWhiteSpace(track.Title) ? track.Title : string.Empty,
                            Artist = !string.IsNullOrWhiteSpace(track.Artist) ? track.Artist : string.Empty,
                            Album = !string.IsNullOrWhiteSpace(track.Album) ? track.Album : string.Empty,
                            AlbumId = !string.IsNullOrWhiteSpace(track.AlbumId) ? track.AlbumId : string.Empty,
                            Duration = (int)track.Duration,
                            Path = !string.IsNullOrWhiteSpace(track.Path) ? track.Path : string.Empty,
                            ServerId = serverId,
                            SnapshotId = snapshotId,
                            AddedAt = DateTime.Now,
                            ArtistId = !string.IsNullOrWhiteSpace(track.ArtistId) ? track.ArtistId : string.Empty,
                            IsRemoved = false,
                            Playlist_SortOrder = playlistSortOrder++,
                            PlaylistId = playlist.Id,
                            Size = track.Size,
                            UserRating = track.Rating,
                            Year = track.Year,
                            AlbumArtist = track.AlbumArtist,
                            AlbumArtistId = track.AlbumArtistId
                        }));

                        if (playlistDtos.Count > 100)
                        {
                            await using var conn = new NpgsqlConnection(_connectionString);
                            await conn.ExecuteBulkInsertAsync(
                                "playlists_subsonic_playlist",
                                playlistDtos,
                                SubsonicPlaylistDto.PlaylistDtoColumnNames, 
                                onConflict: OnConflict.DoNothing);
                            playlistDtos.Clear();
                        }

                        if (trackDtos.Count > 100)
                        {
                            await using var conn = new NpgsqlConnection(_connectionString);
                            await conn.ExecuteBulkInsertAsync(
                                "playlists_subsonic_playlist_track",
                                trackDtos,
                                SubsonicPlaylistTrackDto.PlaylistTrackDtoColumnNames, 
                                onConflict: OnConflict.DoNothing);
                            trackDtos.Clear();
                        }
                        
                        task.Increment(tracks.Count);
                        task.Description(Markup.Escape($"Processing Playlist '{playlist.Name}', {task.Value} of {tracks.Count} processed"));
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.WriteLine(Markup.Escape($"Error: {e.Message}"));
                    }
            
                    totalProgressTask.Value++;
                    totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {playlists.Count} processed"));
                }
            });
        
        if (playlistDtos.Any())
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteBulkInsertAsync(
                "playlists_subsonic_playlist",
                playlistDtos,
                SubsonicPlaylistDto.PlaylistDtoColumnNames, 
                onConflict: OnConflict.DoNothing);
            playlistDtos.Clear();
        }

        if (trackDtos.Any())
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteBulkInsertAsync(
                "playlists_subsonic_playlist_track",
                trackDtos,
                SubsonicPlaylistTrackDto.PlaylistTrackDtoColumnNames, 
                onConflict: OnConflict.DoNothing);
            trackDtos.Clear();
        }
        
        await _subSonicRepository.SetLastSyncTimeAsync(serverId);
        await _snapshotRepository.SetSnapshotCompleteAsync(snapshotId);
    }
}