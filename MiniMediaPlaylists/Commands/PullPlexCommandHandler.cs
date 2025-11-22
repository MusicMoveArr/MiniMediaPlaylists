using System.Net;
using MiniMediaPlaylists.Repositories;
using MiniMediaPlaylists.Services;
using RestSharp;
using Spectre.Console;

namespace MiniMediaPlaylists.Commands;

public class PullPlexCommandHandler
{
    private readonly PlexRepository _plexRepository;
    private readonly SnapshotRepository _snapshotRepository;
    
    public PullPlexCommandHandler(string connectionString)
    {
        _plexRepository = new PlexRepository(connectionString);
        _snapshotRepository = new SnapshotRepository(connectionString);
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
                         
                         //bool isPlayListUpdated = await _plexRepository.IsPlaylistUpdatedAsync(serverUrl, playlist.RatingKey, playlist.AddedAt, playlist.UpdatedAt);
                         //if (!isPlayListUpdated)
                         //{
                         //    continue;
                         //}
                         
                         await _plexRepository.UpsertPlaylistAsync(playlist, serverId, snapshotId);
                     
                         var tracks = await plexApiService.GetPlaylistTracksAsync(serverUrl, token, playlist.RatingKey);
         
                         if (tracks?.MediaContainer?.Metadata == null ||
                             tracks?.MediaContainer?.Metadata?.Any() == false)
                         {
                             continue;
                         }
                         var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Title}', 0 of {tracks.MediaContainer.Metadata.Count} processed"));
                         task.MaxValue = tracks.MediaContainer.Metadata.Count;
                         
                         foreach (var track in tracks.MediaContainer.Metadata)
                         {
                             task.Value++;
                             task.Description(Markup.Escape($"Processing Playlists '{playlist.Title}', {task.Value} of {tracks.MediaContainer.Metadata.Count} processed"));
                             await _plexRepository.UpsertPlaylistTrackAsync(track, playlist.RatingKey, serverId, snapshotId);
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

        await _plexRepository.SetLastSyncTimeAsync(serverId);
        await _snapshotRepository.SetSnapshotCompleteAsync(snapshotId);
    }
}