using System.Security.Cryptography;
using System.Text;
using MiniMediaPlaylists.Models.SubsonicDto;
using MiniMediaPlaylists.Repositories;
using Npgsql;
using Spectre.Console;
using SubSonicMedia;
using SubSonicMedia.Models;
using SubSonicMedia.Responses.Playlists.Models;
using SubSonicMedia.Responses.Search.Models;
using DapperBulkQueries.Common;
using DapperBulkQueries.Npgsql;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Services;

namespace MiniMediaPlaylists.Commands;

public class PullSubSonicCommandHandler
{
    private readonly string _connectionString;
    private readonly SubSonicRepository _subSonicRepository;
    private readonly SnapshotRepository _snapshotRepository;
    private readonly SnapshotRetentionService _snapshotRetentionService;

    public PullSubSonicCommandHandler(string connectionString)
    {
        _connectionString = connectionString;
        _subSonicRepository = new SubSonicRepository(connectionString);
        _snapshotRepository = new SnapshotRepository(connectionString);
        _snapshotRetentionService = new SnapshotRetentionService();
    }

    public async Task PullSubSonicPlaylists(
        string serverUrl, 
        string username, 
        string password, 
        string likedSongsPlaylistName, 
        RetentionPolicy retentionPolicy)
    {
        var connection = new SubsonicConnectionInfo(
            serverUrl: serverUrl,
            username: username,
            password: password
        );
        using var client = new SubsonicClient(connection);
        var playlists = await client.Playlists.GetPlaylistsAsync();

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
                var totalProgressTask = ctx.AddTask(Markup.Escape($"Processing Playlists 0 of {playlists.Playlists.Playlist.Count} processed"));
                totalProgressTask.MaxValue = playlists.Playlists.Playlist.Count;

                string genLikedPlaylistName = string.Empty;
                
                if (!string.IsNullOrWhiteSpace(likedSongsPlaylistName))
                {
                    string uniqueHashId =
                        BitConverter.ToString(SHA256.Create()
                            .ComputeHash(Encoding.UTF8.GetBytes(likedSongsPlaylistName)))
                            .Replace("-", string.Empty);
                    
                    genLikedPlaylistName = $"#{uniqueHashId}";
                    playlists.Playlists.Playlist.Insert(0, new PlaylistSummary
                    {
                        Changed = DateTime.Now,
                        Created = DateTime.Now,
                        Duration = 0,
                        Id = genLikedPlaylistName,
                        Name = likedSongsPlaylistName,
                        Public = false,
                        SongCount = 0
                    });
                }

                foreach (var playlist in playlists.Playlists.Playlist)
                {
                    try
                    {
                        playlistDtos.Add(new SubsonicPlaylistDto
                        {
                            Id = playlist.Id,
                            Name = !string.IsNullOrWhiteSpace(playlist.Name) ? playlist.Name : string.Empty,
                            Owner = !string.IsNullOrWhiteSpace(playlist.Owner) ? playlist.Owner : string.Empty,
                            Public = playlist.Public,
                            SongCount = playlist.SongCount,
                            ChangedAt = playlist.Changed,
                            CreatedAt = playlist.Created,
                            Comment = !string.IsNullOrWhiteSpace(playlist.Comment) ? playlist.Comment : string.Empty,
                            Duration = playlist.Duration,
                            ServerId = serverId,
                            SnapshotId = snapshotId
                        });
                        
                        List<Song> tracks = new List<Song>();
                        if (!string.IsNullOrWhiteSpace(genLikedPlaylistName) &&
                            string.Equals(genLikedPlaylistName, playlist.Id))
                        {
                            //personally I don't get the UserRating/AverageRating, it's always NULL
                            //for now only "starring" or liking the song on another service works
                            var starredTracks = await client.Browsing.GetStarredAsync();
                            tracks = starredTracks.Starred.Song.ToList();
                        }
                        else
                        {
                            tracks = client.Playlists.GetPlaylistAsync(playlist.Id).Result.Playlist.Entry;
                        }
                        
                        var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Name}', 0 of {tracks.Count} processed"));
                        task.MaxValue = tracks.Count;
                        int playlistSortOrder = 1;
                        
                        trackDtos.AddRange(tracks.Select(track => new SubsonicPlaylistTrackDto
                        {
                            Id = track.Id,
                            Title = !string.IsNullOrWhiteSpace(track.Title) ? track.Title : string.Empty,
                            Artist = !string.IsNullOrWhiteSpace(track.Artist) ? track.Artist : string.Empty,
                            Album = !string.IsNullOrWhiteSpace(track.Album) ? track.Album : string.Empty,
                            AlbumId = !string.IsNullOrWhiteSpace(track.AlbumId) ? track.AlbumId : string.Empty,
                            Duration = track.Duration,
                            Path = !string.IsNullOrWhiteSpace(track.Path) ? track.Path : string.Empty,
                            ServerId = serverId,
                            SnapshotId = snapshotId,
                            AddedAt = DateTime.Now,
                            ArtistId = !string.IsNullOrWhiteSpace(track.ArtistId) ? track.ArtistId : string.Empty,
                            IsRemoved = false,
                            Playlist_SortOrder = playlistSortOrder++,
                            PlaylistId = playlist.Id,
                            Size = track.Size,
                            UserRating = track.UserRating ?? 0,
                            Year = track.Year ?? 0
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
                    totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {playlists.Playlists.Playlist.Count} processed"));
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