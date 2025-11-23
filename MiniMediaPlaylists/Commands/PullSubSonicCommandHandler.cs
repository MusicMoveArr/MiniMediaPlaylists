using System.Security.Cryptography;
using System.Text;
using MiniMediaPlaylists.Repositories;
using Spectre.Console;
using SubSonicMedia;
using SubSonicMedia.Models;
using SubSonicMedia.Responses.Playlists.Models;
using SubSonicMedia.Responses.Search.Models;

namespace MiniMediaPlaylists.Commands;

public class PullSubSonicCommandHandler
{
    private readonly SubSonicRepository _subSonicRepository;
    private readonly SnapshotRepository _snapshotRepository;
    public PullSubSonicCommandHandler(string connectionString)
    {
        _subSonicRepository = new SubSonicRepository(connectionString);
        _snapshotRepository = new SnapshotRepository(connectionString);
    }

    public async Task PullSubSonicPlaylists(
        string serverUrl, 
        string username, 
        string password, 
        string likedSongsPlaylistName)
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
                        await _subSonicRepository.UpsertPlaylistAsync(playlist.Id, 
                            serverId, 
                            playlist.Changed,
                            playlist.Created,
                            playlist.Comment,
                            playlist.Duration,
                            playlist.Name,
                            playlist.Owner,
                            playlist.Public,
                            playlist.SongCount,
                            snapshotId);

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
                        foreach (var track in tracks)
                        {
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
                                DateTime.Now,
                                track.UserRating ?? 0,
                                snapshotId,
                                playlistSortOrder);
                            playlistSortOrder++;
                            task.Increment(1);
                            task.Description(Markup.Escape($"Processing Playlist '{playlist.Name}', {task.Value} of {tracks.Count} processed"));
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
        await _snapshotRepository.SetSnapshotCompleteAsync(snapshotId);
    }
}