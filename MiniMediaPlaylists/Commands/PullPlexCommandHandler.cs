using System.Net;
using DapperBulkQueries.Common;
using DapperBulkQueries.Npgsql;
using MiniMediaPlaylists.Models.PlexDto;
using MiniMediaPlaylists.Repositories;
using MiniMediaPlaylists.Services;
using Npgsql;
using RestSharp;
using Spectre.Console;

namespace MiniMediaPlaylists.Commands;

public class PullPlexCommandHandler
{
    private const int MinimumBulkInsert = 100;
    private readonly string _connectionString;
    private readonly PlexRepository _plexRepository;
    private readonly SnapshotRepository _snapshotRepository;
    private readonly List<PlexPlaylistDto> _playlistDtos;
    private readonly List<PlexPlaylistTrackDto> _trackDtos;
    
    public PullPlexCommandHandler(string connectionString)
    {
        _connectionString = connectionString;
        _plexRepository = new PlexRepository(connectionString);
        _snapshotRepository = new SnapshotRepository(connectionString);
        _playlistDtos = new List<PlexPlaylistDto>();
        _trackDtos = new List<PlexPlaylistTrackDto>();
    }

    public async Task PullPlexPlaylists(string serverUrl, string token, int trackLimit)
    {
        PlexApiService plexApiService = new PlexApiService();
        var playlists = await plexApiService.GetPlaylistsAsync(serverUrl, token);

        var serverId = await _plexRepository.UpsertServerAsync(serverUrl);
        Guid snapshotId = await _snapshotRepository.CreateSnapshotAsync(serverId, "Plex");
        
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
                var totalProgressTask = ctx.AddTask(Markup.Escape($"Processing Playlists 0 of {playlists.MediaContainer.Metadata.Count} processed"));
                totalProgressTask.MaxValue = playlists.MediaContainer.Metadata.Count;
                 foreach (var playlist in playlists.MediaContainer.Metadata)
                 {
                     try
                     {
                         if (playlist.LeafCount > trackLimit)
                         {
                             continue;
                         }
                         
                         _playlistDtos.Add(new PlexPlaylistDto
                         {
                             RatingKey = playlist.RatingKey,
                             ServerId = serverId,
                             Key = playlist.Key,
                             Guid = playlist.Guid,
                             Type = playlist.Type,
                             Title = playlist.Title,
                             TitleSort = !string.IsNullOrWhiteSpace(playlist.TitleSort) ? playlist.TitleSort : string.Empty,
                             Summary = playlist.Summary,
                             Smart = playlist.Smart,
                             PlaylistType = playlist.PlaylistType,
                             Composite = !string.IsNullOrWhiteSpace(playlist.Composite) ? playlist.Composite : string.Empty,
                             Icon = !string.IsNullOrWhiteSpace(playlist.Icon) ? playlist.Icon : string.Empty,
                             LastViewedAt = DateTimeOffset.FromUnixTimeSeconds(playlist.LastViewedAt).DateTime,
                             Duration = playlist.Duration,
                             LeafCount = playlist.LeafCount,
                             AddedAt = DateTimeOffset.FromUnixTimeSeconds(playlist.AddedAt).DateTime,
                             UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(playlist.UpdatedAt).DateTime,
                             SnapshotId = snapshotId
                         });
                         await BulkInsertPlaylistsAsync(MinimumBulkInsert);

                         var tracks = await plexApiService.GetPlaylistTracksAsync(serverUrl, token, playlist.RatingKey);
         
                         if (tracks?.MediaContainer?.Metadata == null ||
                             tracks?.MediaContainer?.Metadata?.Any() == false)
                         {
                             continue;
                         }
                         var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Title}', 0 of {tracks.MediaContainer.Metadata.Count} processed"));
                         task.MaxValue = tracks.MediaContainer.Metadata.Count;

                         int playlistSortOrder = 1;
                         foreach (var track in tracks.MediaContainer.Metadata)
                         {
                             task.Value++;
                             task.Description(Markup.Escape($"Processing Playlists '{playlist.Title}', {task.Value} of {tracks.MediaContainer.Metadata.Count} processed"));
                             
                             _trackDtos.Add(new PlexPlaylistTrackDto
                             {
                                 RatingKey = track.RatingKey,
                                 PlaylistId = playlist.RatingKey,
                                 ServerId = serverId,
                                 Key = track.Key,
                                 Type = track.Type,
                                 Title = track.Title,
                                 Guid = track.Guid,
                                 ParentStudio = !string.IsNullOrWhiteSpace(track.ParentStudio) ? track.ParentStudio : string.Empty,
                                 LibrarySectionTitle = track.LibrarySectionTitle,
                                 LibrarySectionId = track.LibrarySectionId,
                                 GrandParentTitle = track.GrandparentTitle,
                                 UserRating = track.UserRating,
                                 ParentTitle = !string.IsNullOrWhiteSpace(track.ParentTitle) ? track.ParentTitle : string.Empty,
                                 ParentYear = track.ParentYear,
                                 MusicAnalysisVersion = !string.IsNullOrWhiteSpace(track.MusicAnalysisVersion) ? int.Parse(track.MusicAnalysisVersion) : 0,
                                 MediaId = track.Media.First().Id,
                                 MediaPartId = track.Media.First().Part.First().Id,
                                 MediaPartKey = track.Media.First().Part.First().Key,
                                 MediaPartDuration = track.Media.First().Part.First().Duration,
                                 MediaPartFile = track.Media.First().Part.First().File,
                                 MediaPartContainer = track.Media.First().Part.First().Container,
                                 IsRemoved = false,
                                 LastViewedAt = DateTimeOffset.FromUnixTimeSeconds(track.LastViewedAt).DateTime,
                                 LastRatedAt = DateTimeOffset.FromUnixTimeSeconds(track.LastRatedAt).DateTime,
                                 AddedAt = DateTimeOffset.FromUnixTimeSeconds(track.AddedAt).DateTime,
                                 SnapshotId = snapshotId,
                                 Playlist_SortOrder = playlistSortOrder,
                                 Playlist_ItemId = track.PlaylistItemId
                             });

                             await BulkInsertTracksAsync(MinimumBulkInsert);
                             
                             playlistSortOrder++;
                         }
                     }
                     catch (Exception e)
                     {
                         Console.WriteLine(e.Message);
                     }
                     totalProgressTask.Value++;
                     totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {playlists.MediaContainer.Metadata.Count} processed"));
                 }               
            });

        await BulkInsertPlaylistsAsync(0);
        await BulkInsertTracksAsync(0);
        
        await _plexRepository.SetLastSyncTimeAsync(serverId);
        await _snapshotRepository.SetSnapshotCompleteAsync(snapshotId);
    }
    
    
    private async Task BulkInsertPlaylistsAsync(int minimumRecords)
    {
        if (_playlistDtos.Count > minimumRecords)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteBulkInsertAsync(
                "playlists_plex_playlist",
                _playlistDtos,
                PlexPlaylistDto.PlaylistDtoColumnNames, 
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
                "playlists_plex_playlist_track",
                _trackDtos,
                PlexPlaylistTrackDto.PlaylistTrackDtoColumnNames, 
                onConflict: OnConflict.DoNothing);
            _trackDtos.Clear();
        }
    }
}