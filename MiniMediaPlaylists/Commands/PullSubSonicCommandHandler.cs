using MiniMediaPlaylists.Repositories;
using Spectre.Console;
using SubSonicMedia;
using SubSonicMedia.Models;

namespace MiniMediaPlaylists.Commands;

public class PullSubSonicCommandHandler
{
    private readonly SubSonicRepository _subSonicRepository;
    public PullSubSonicCommandHandler(string connectionString)
    {
        _subSonicRepository = new SubSonicRepository(connectionString);
    }

    public async Task PullSubSonicPlaylists(string serverUrl, string username, string password)
    {
        var connection = new SubsonicConnectionInfo(
            serverUrl: serverUrl,
            username: username,
            password: password
        );
        using var client = new SubsonicClient(connection);
        var playlists = await client.Playlists.GetPlaylistsAsync();

        Guid serverId = await _subSonicRepository.UpsertServerAsync(serverUrl);

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
                
                foreach (var playlist in playlists.Playlists.Playlist)
                {
                    
                    try
                    {
                        await _subSonicRepository.UpsertPlaylistAsync(playlist.Id, 
                            serverId, 
                            playlist.Changed,
                            playlist.Created,
                            playlist.Comment,
                            playlist.Duration,
                            playlist.Name,
                            playlist.Owner,
                            playlist.Public,
                            playlist.SongCount);
            
                        var tracks = client.Playlists.GetPlaylistAsync(playlist.Id).Result.Playlist.Entry;
                        var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Name}', 0 of {tracks.Count} processed"));
                        task.MaxValue = tracks.Count;
                        
                        foreach (var track in tracks)
                        {
                            task.Value++;
                            
                            await _subSonicRepository.UpsertPlaylistTrackAsync(
                                track.Id,
                                serverId,
                                playlist.Id,
                                track.Album,
                                track.AlbumId,
                                track.Artist,
                                track.ArtistId,
                                track.Duration,
                                track.Title,
                                track.Path,
                                track.Size,
                                track.Year ?? 0,
                                DateTime.Now);
                        }
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.WriteLine(Markup.Escape($"Error: {e.Message}"));
                    }
            
                    totalProgressTask.Value++;
                    totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {playlists.Playlists.Playlist.Count} processed"));
                }
            });
        await _subSonicRepository.SetLastSyncTimeAsync(serverId);
    }
}