using System.Security.Cryptography;
using System.Text;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Models.Jellyfin;
using MiniMediaPlaylists.Repositories;
using MiniMediaPlaylists.Services;
using Spectre.Console;

namespace MiniMediaPlaylists.Commands;

public class PullJellyfinCommandHandler
{
    private readonly JellyfinRepository _jellyfinRepository;
    private readonly SnapshotRepository _snapshotRepository;
    private readonly SnapshotRetentionService _snapshotRetentionService;
    
    public PullJellyfinCommandHandler(string connectionString)
    {
        _jellyfinRepository = new JellyfinRepository(connectionString);
        _snapshotRepository = new SnapshotRepository(connectionString);
        _snapshotRetentionService = new SnapshotRetentionService();
    }

    public async Task PullJellyfinPlaylists(
        string serverUrl, 
        string username, 
        string password, 
        string favoriteSongsPlaylistName, 
        RetentionPolicy retentionPolicy)
    {
        JellyfinApiService jellyfinApiService = new JellyfinApiService();
        
        var dbAuthInfo = await _jellyfinRepository.GetOwnerByNameAsync(username, serverUrl);
        string? accessToken = dbAuthInfo?.AccessToken;
        string? jellyfinUserId = dbAuthInfo?.JellyfinUserId;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            var auth = await jellyfinApiService.AuthenticateAsync(serverUrl, username, password);
            await _jellyfinRepository.UpsertOwnerAsync(auth.User.Name, auth.User.Id, auth.AccessToken, serverUrl);
            accessToken = auth.AccessToken;
            jellyfinUserId = auth.User.Id;
            dbAuthInfo = await _jellyfinRepository.GetOwnerByNameAsync(username, serverUrl);
        }
        
        var playlists = await jellyfinApiService
            .GetItems<JellyfinPlaylistItem>(serverUrl, jellyfinUserId, accessToken, "Playlist", true, false);

        Guid snapshotId = await _snapshotRepository.CreateSnapshotAsync(dbAuthInfo.Id, "Jellyfin");

        var allSnapshots = await _snapshotRepository.GetSnapshotsByServerIdAsync(dbAuthInfo.Id);
        var snapshotIdsToCleanup = _snapshotRetentionService.GetSnapshotsToRemove(allSnapshots, retentionPolicy);
        await _jellyfinRepository.DeleteSnapshotsAsync(snapshotIdsToCleanup);
        await _snapshotRepository.DeleteSnapshotsAsync(snapshotIdsToCleanup);
        
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
                var totalProgressTask = ctx.AddTask(Markup.Escape($"Processing Playlists 0 of {playlists.Items.Count} processed"));
                totalProgressTask.MaxValue = playlists.Items.Count;
                
                if (!string.IsNullOrWhiteSpace(favoriteSongsPlaylistName))
                {
                    var favoriteTracks = await jellyfinApiService
                        .GetItems<JellyfinTrackItem>(serverUrl, jellyfinUserId, accessToken, "Audio", true, true);
                    
                    string uniqueHashId =
                        BitConverter.ToString(SHA256.Create()
                                .ComputeHash(Encoding.UTF8.GetBytes(favoriteSongsPlaylistName)))
                            .Replace("-", string.Empty);
                    string genFavoritePlaylistId = $"#{uniqueHashId}";
                    
                    await _jellyfinRepository.UpsertPlaylistAsync(
                        genFavoritePlaylistId, 
                        dbAuthInfo.Id, 
                        favoriteSongsPlaylistName, 
                        string.Empty,
                        string.Empty, 
                        true, 
                        string.Empty, 
                        string.Empty,
                        string.Empty,
                        snapshotId);
                    
                    var task = ctx.AddTask(Markup.Escape($"Processing Favorites Playlist '{favoriteSongsPlaylistName}', 0 of {favoriteTracks.Items.Count} processed"));
                    task.MaxValue = favoriteTracks.Items.Count;

                    foreach (var track in favoriteTracks.Items)
                    {
                        await _jellyfinRepository.UpsertPlaylistTrackAsync(track.Id, 
                            genFavoritePlaylistId, 
                            dbAuthInfo.Id, 
                            track.Name,
                            !string.IsNullOrWhiteSpace(track.Artists.FirstOrDefault()) ? track.Artists.FirstOrDefault() : string.Empty, 
                            track.AlbumArtist,  
                            track.Album, 
                            string.Empty, 
                            track.Container,
                            track.PremiereDate ?? new DateTime(2000, 01, 01), 
                            !string.IsNullOrWhiteSpace(track.ChannelId) ? track.ChannelId : string.Empty, 
                            track.ProductionYear, 
                            track.IndexNumber, 
                            track.IsFolder, 
                            track.UserData.Key,
                            track.UserData.IsFavorite,
                            track.MediaType, 
                            track.LocationType, 
                            false, 
                            DateTime.Now,
                            snapshotId);
                        task.Increment(1);
                        task.Description(Markup.Escape($"Processing Favorites Playlist '{favoriteSongsPlaylistName}', {task.Value} of {favoriteTracks.Items.Count} processed"));
                    }
                }

                foreach (var playlist in playlists.Items)
                {
                    try
                    {
                        await _jellyfinRepository.UpsertPlaylistAsync(playlist.Id,
                            dbAuthInfo.Id,
                            playlist.Name,
                            playlist.ServerId,
                            !string.IsNullOrWhiteSpace(playlist.ChannelId) ? playlist.ChannelId : string.Empty,
                            playlist.IsFolder,
                            playlist.UserData.Key,
                            playlist.MediaType,
                            playlist.LocationType,
                            snapshotId);

                        var tracks = await jellyfinApiService.GetPlaylistTracks(serverUrl, dbAuthInfo.JellyfinUserId,
                            playlist.Id,
                            dbAuthInfo.AccessToken);

                        
                        var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{playlist.Name}', 0 of {tracks.Items.Count} processed"));
                        task.MaxValue = tracks.Items.Count;
                        
                        foreach (var track in tracks.Items)
                        {
                            await _jellyfinRepository.UpsertPlaylistTrackAsync(track.Id,
                                playlist.Id,
                                dbAuthInfo.Id,
                                track.Name,
                                !string.IsNullOrWhiteSpace(track.Artists.FirstOrDefault()) ? track.Artists.FirstOrDefault() : string.Empty,
                                track.AlbumArtist,
                                track.Album,
                                track.PlayListItemId,
                                track.Container,
                                track.PremiereDate ?? new DateTime(2000, 01, 01),
                                !string.IsNullOrWhiteSpace(track.ChannelId) ? track.ChannelId : string.Empty,
                                track.ProductionYear,
                                track.IndexNumber,
                                track.IsFolder,
                                track.UserData.Key,
                                track.UserData.IsFavorite,
                                track.MediaType,
                                track.LocationType, 
                                false, 
                                DateTime.Now,
                                snapshotId);
                            task.Increment(1);
                            task.Description(Markup.Escape($"Processing Playlist '{playlist.Name}', {task.Value} of {tracks.Items.Count} processed"));
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.WriteLine(Markup.Escape($"Error: {ex.Message}"));
                    }
                    
                    totalProgressTask.Value++;
                    totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {playlists.Items.Count} processed"));

                }
            });

        await _jellyfinRepository.SetLastSyncTimeAsync(dbAuthInfo.Id);
        await _snapshotRepository.SetSnapshotCompleteAsync(snapshotId);
    }
}