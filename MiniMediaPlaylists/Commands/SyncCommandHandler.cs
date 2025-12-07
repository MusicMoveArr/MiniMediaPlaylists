using FuzzySharp;
using MiniMediaPlaylists.Helpers;
using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Repositories;
using MiniMediaPlaylists.Services;
using Spectre.Console;

namespace MiniMediaPlaylists.Commands;

public class SyncCommandHandler
{
    private readonly string _connectionString;
    private IProviderService _fromProvider;
    private IProviderService _toProvider;
    private readonly SnapshotRepository _snapshotRepository;
    private const int MaxMovingPlaylistTracksLoop = 10;
    
    public SyncCommandHandler(string connectionString)
    {
        _connectionString = connectionString;
        _snapshotRepository = new SnapshotRepository(connectionString);
    }

    public async Task SyncPlaylists(SyncConfiguration syncConfiguration)
    {
        _fromProvider = GetProviderServiceFrom(syncConfiguration, _connectionString);
        _toProvider = GetProviderServiceTo(syncConfiguration, _connectionString);

        Guid fromSnapshotId = await GetLastCompleteTransactionAsync(syncConfiguration.FromService, syncConfiguration.FromName) ?? Guid.Empty;
        Guid toSnapshotId = await GetLastCompleteTransactionAsync(syncConfiguration.ToService, syncConfiguration.ToName) ?? Guid.Empty;

        var fromPlaylists = await _fromProvider.GetPlaylistsAsync(syncConfiguration.FromName, fromSnapshotId);
        var toPlaylists = await _toProvider.GetPlaylistsAsync(syncConfiguration.ToName, toSnapshotId);
        
        if (!string.IsNullOrWhiteSpace(syncConfiguration.FromPlaylistName))
        {
            fromPlaylists = fromPlaylists
                .Where(playlist => string.Equals(playlist.Name, syncConfiguration.FromPlaylistName))
                .ToList();
        }
        
        fromPlaylists = fromPlaylists
            .Where(playlist => !syncConfiguration.FromSkipPlaylists.Contains(playlist.Name))
            .ToList();
        
        fromPlaylists = fromPlaylists
            .Where(playlist => !syncConfiguration.FromSkipPrefixPlaylists.Any(prefix => playlist.Name.StartsWith(prefix)))
            .ToList();
        
        if (!string.IsNullOrWhiteSpace(syncConfiguration.ToPlaylistName))
        {
            toPlaylists = toPlaylists
                .Where(playlist => string.Equals(playlist.Name, syncConfiguration.ToPlaylistPrefix + syncConfiguration.ToPlaylistName))
                .ToList();
        }

        if (!fromPlaylists.Any())
        {
            Console.WriteLine($"No playlists found in '{syncConfiguration.FromService}' named '{syncConfiguration.FromPlaylistName}'");
            return;
        }
        
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
                var totalProgressTask = ctx.AddTask(Markup.Escape($"Processing Playlists 0 of {fromPlaylists.Count} processed"));
                totalProgressTask.MaxValue = fromPlaylists.Count;
                
                int playlistProgress = 0;
                await ParallelHelper.ForEachAsync(fromPlaylists, syncConfiguration.PlaylistThreads, async fromPlaylist =>
                {
                    var toPlayList = toPlaylists.FirstOrDefault(playlist => 
                        string.Equals(playlist.Name, syncConfiguration.ToPlaylistPrefix + fromPlaylist.Name));

                    bool isLikePlaylist = string.Equals(fromPlaylist.Name, syncConfiguration.FromLikePlaylistName);
                    
                    if (toPlayList?.CanAddTracks == false && !isLikePlaylist)
                    {
                        return;
                    }
                    
                    if (toPlayList == null)
                    {
                        //create non-existing playlist on "to" service
                        toPlayList = await _toProvider.CreatePlaylistAsync(syncConfiguration.ToName,
                            syncConfiguration.ToPlaylistPrefix + fromPlaylist.Name);
                    }

                    var fromTracks = await _fromProvider.GetPlaylistTracksAsync(syncConfiguration.FromName, fromPlaylist.Id, fromSnapshotId);
                    var toTracks =
                        isLikePlaylist ? await _toProvider.GetPlaylistTracksByNameAsync(syncConfiguration.ToName, syncConfiguration.ToLikePlaylistName, toSnapshotId) :
                        await _toProvider.GetPlaylistTracksAsync(syncConfiguration.ToName, toPlayList.Id, toSnapshotId);

                    var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{fromPlaylist.Name}', 0 of {fromTracks.Count} processed"));
                    task.MaxValue = fromTracks.Count;

                    List<UpdatePlaylistTrackOrder> updatePlaylistTrackOrders = new List<UpdatePlaylistTrackOrder>();
                    
                    await ParallelHelper.ForEachAsync(fromTracks, syncConfiguration.TrackThreads, async fromTrack =>
                    {
                        try
                        {
                            if (!syncConfiguration.ForceAddTrack)
                            {
                                var toTrack = FindTrack(toTracks, syncConfiguration, fromTrack, false);

                                if (toTrack == null && syncConfiguration.SecondSearchWithoutAlbum)
                                {
                                    toTrack = FindTrack(toTracks, syncConfiguration, fromTrack, true);
                                }
                                
                                if (toTrack != null)
                                {
                                    if (syncConfiguration.SyncTrackOrder)
                                    {
                                        updatePlaylistTrackOrders.Add(new UpdatePlaylistTrackOrder
                                        {
                                            ToName = syncConfiguration.ToName,
                                            ToPlaylist = toPlayList,
                                            FromTrack = fromTrack,
                                            ToTrack = toTrack,
                                            NewPlaylistSortOrder = fromTrack.PlaylistSortOrder
                                        });
                                    }
                                    
                                    task.Value++;
                                    task.Description(Markup.Escape(Markup.Escape($"Processing Playlist '{fromPlaylist.Name}', {task.Value} of {fromTracks.Count} processed")));
                                    return;
                                }
                            }

                            var searchResults = await _toProvider.SearchTrackAsync(
                                syncConfiguration.ToName,
                                fromTrack.ArtistName,
                                fromTrack.AlbumName,
                                fromTrack.Title);

                            var foundTrack = FindTrack(searchResults, syncConfiguration, fromTrack, false);

                            bool foundWithDeepSearch = false;
                            if (foundTrack == null && syncConfiguration.DeepSearchThroughArtist)
                            {
                                searchResults = await _toProvider.DeepSearchTrackAsync(
                                    syncConfiguration.ToName,
                                    fromTrack.ArtistName,
                                    fromTrack.AlbumName,
                                    fromTrack.Title,
                                    toSnapshotId);
                                foundTrack = FindTrack(searchResults, syncConfiguration, fromTrack, false);
                                foundWithDeepSearch = foundTrack != null;
                            }

                            if (foundTrack == null && syncConfiguration.SecondSearchWithoutAlbum)
                            {
                                searchResults = await _toProvider.SearchTrackAsync(
                                    syncConfiguration.ToName,
                                    fromTrack.ArtistName,
                                    string.Empty,
                                    fromTrack.Title);

                                foundTrack = FindTrack(searchResults, syncConfiguration, fromTrack, true);

                                foundWithDeepSearch = false;
                                if (foundTrack == null && syncConfiguration.DeepSearchThroughArtist)
                                {
                                    searchResults = await _toProvider.DeepSearchTrackAsync(
                                        syncConfiguration.ToName,
                                        fromTrack.ArtistName,
                                        string.Empty,
                                        fromTrack.Title,
                                        toSnapshotId);
                                    foundTrack = FindTrack(searchResults, syncConfiguration, fromTrack, true);
                                    foundWithDeepSearch = foundTrack != null;
                                }
                            }

                            if (foundTrack != null)
                            {
                                if (syncConfiguration.SyncTrackOrder)
                                {
                                    updatePlaylistTrackOrders.Add(new UpdatePlaylistTrackOrder
                                    {
                                        ToName = syncConfiguration.ToName,
                                        ToPlaylist = toPlayList,
                                        FromTrack = fromTrack,
                                        ToTrack = foundTrack,
                                        NewPlaylistSortOrder = fromTrack.PlaylistSortOrder
                                    });
                                }
                                
                                if (await _toProvider.RateTrackAsync(syncConfiguration.ToName, foundTrack, fromTrack.LikeRating))
                                {
                                    AnsiConsole.WriteLine(Markup.Escape($"Rated song with rating '{fromTrack.LikeRating}' '{foundTrack.ArtistName} - {foundTrack.AlbumName} - {foundTrack.Title}' {(foundWithDeepSearch ? "found with deep search" : "")}"));
                                }

                                if (isLikePlaylist)
                                {
                                    if (await _toProvider.LikeTrackAsync(syncConfiguration.ToName, foundTrack, fromTrack.LikeRating))
                                    {
                                        AnsiConsole.WriteLine(Markup.Escape($"Liked song with rating '{fromTrack.LikeRating}' '{foundTrack.ArtistName} - {foundTrack.AlbumName} - {foundTrack.Title}' {(foundWithDeepSearch ? "found with deep search" : "")}"));
                                    }
                                    else
                                    {
                                        AnsiConsole.WriteLine(Markup.Escape($"Failed to like the song, '{foundTrack.ArtistName} - {foundTrack.AlbumName} - {foundTrack.Title}'"));
                                    }
                                }
                                else
                                {
                                    await _toProvider.AddTrackToPlaylistAsync(syncConfiguration.ToName, toPlayList.Id, foundTrack);
                                    AnsiConsole.WriteLine(Markup.Escape($"Added song to playlist '{foundTrack.ArtistName} - {foundTrack.AlbumName} - {foundTrack.Title}' {(foundWithDeepSearch ? "found with deep search" : "")}"));
                                }
                            }
                            else
                            {
                                AnsiConsole.WriteLine(Markup.Escape($"Track not found for '{syncConfiguration.ToService}',      {fromTrack.ArtistName} <!-!> {fromTrack.AlbumName} <!-!> {fromTrack.Title}"));
                            }
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Error: {e.Message}"));
                        }

                        task.Value++;
                        task.Description(Markup.Escape(Markup.Escape($"Processing Playlist '{fromPlaylist.Name}', {task.Value} of {fromTracks.Count} processed")));
                    });

                    if (syncConfiguration.SyncTrackOrder && !isLikePlaylist && toPlayList.CanSortTracks)
                    {
                        await FixPlaylistTrackOrderingAsync(updatePlaylistTrackOrders, syncConfiguration, fromTracks, toPlayList);
                    }
                    
                    totalProgressTask.Value++;
                    totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {fromPlaylists.Count} processed"));
                });
            });
    }
    
    private async Task FixPlaylistTrackOrderingAsync(
        List<UpdatePlaylistTrackOrder> updatePlaylistTrackOrders,
        SyncConfiguration syncConfiguration,
        List<GenericTrack> fromTracks,
        GenericPlaylist toPlaylist)
    {
        updatePlaylistTrackOrders = updatePlaylistTrackOrders
            .OrderByDescending(t => t.NewPlaylistSortOrder)
            .ToList();
        
        fromTracks = fromTracks
            .OrderByDescending(t => t.PlaylistSortOrder)
            .ToList();
        
        if (updatePlaylistTrackOrders.Count != fromTracks.Count)
        {
            return;
        }
        
        var tracksToOrder = updatePlaylistTrackOrders
            .Where(t => t.ToTrack.PlaylistSortOrder != t.NewPlaylistSortOrder)
            .OrderByDescending(t => t.NewPlaylistSortOrder)
            .ToList();
        
        //dictionary that will remember how many times we moved a track in the playlist
        //if a track moved >10x for whatever reason it's stuck in a weird loop and we'll quit ordering
        Dictionary<string, Dictionary<int, int>> orderedTrackCount = new Dictionary<string, Dictionary<int, int>>();
        
        while(tracksToOrder.Count > 0)
        {
            var track = tracksToOrder.First();
            
            var toPlaylistTracks = updatePlaylistTrackOrders
                .Select(t => t.ToTrack)
                .ToList();
            
            await _toProvider.SetTrackPlaylistOrderAsync(syncConfiguration.ToName, toPlaylist, track.ToTrack, toPlaylistTracks, track.NewPlaylistSortOrder);

            //track moved down in playlist
            if (track.NewPlaylistSortOrder > track.ToTrack.PlaylistSortOrder)
            {
                var moveTracksList = updatePlaylistTrackOrders
                    .Where(t => t.ToTrack.Id != track.ToTrack.Id)
                    .Where(t => t.ToTrack.PlaylistSortOrder <= track.NewPlaylistSortOrder)
                    .Where(t => t.ToTrack.PlaylistSortOrder >= track.ToTrack.PlaylistSortOrder)
                    .ToList();
                
                foreach (var movedTrack in moveTracksList)
                {
                    movedTrack.ToTrack.PlaylistSortOrder--;
                }
            }
            else //track moved up in playlist
            {
                var moveTracksList = updatePlaylistTrackOrders
                    .Where(t => t.ToTrack.Id != track.ToTrack.Id)
                    .Where(t => t.ToTrack.PlaylistSortOrder >= track.NewPlaylistSortOrder)
                    .Where(t => t.ToTrack.PlaylistSortOrder <= track.ToTrack.PlaylistSortOrder)
                    .ToList();

                foreach (var movedTrack in moveTracksList.OrderBy(x => x.ToTrack.PlaylistSortOrder))
                {
                    movedTrack.ToTrack.PlaylistSortOrder++;
                }
            }
            track.ToTrack.PlaylistSortOrder = track.NewPlaylistSortOrder;
            
            Dictionary<int, int> orderingHistory = new Dictionary<int, int>();
            if (!orderedTrackCount.TryGetValue(track.ToTrack.Id, out orderingHistory))
            {
                orderingHistory = new Dictionary<int, int>();
                orderedTrackCount.Add(track.ToTrack.Id, orderingHistory);
            }

            if (!orderingHistory.ContainsKey(track.NewPlaylistSortOrder))
            {
                orderingHistory.Add(track.NewPlaylistSortOrder, 0);
            }
            orderingHistory[track.NewPlaylistSortOrder]++;

            if (orderingHistory[track.NewPlaylistSortOrder] >= MaxMovingPlaylistTracksLoop)
            {
                break;
            }
            
            tracksToOrder = updatePlaylistTrackOrders
                .Where(t => t.ToTrack.PlaylistSortOrder != t.NewPlaylistSortOrder)
                .OrderByDescending(t => t.NewPlaylistSortOrder)
                .ToList();
        }
    }

    private GenericTrack? FindTrack(
        List<GenericTrack> searchResults,
        SyncConfiguration syncConfiguration,
        GenericTrack fromTrack,
        bool ignoreAlbum)
    {
        var foundTracks = searchResults
            .Select(track => new
            {
                Track = track,
                ArtistMatch = Fuzz.PartialRatio(track.ArtistName.ToLower(), fromTrack.ArtistName.ToLower()),
                AlbumMatch = ignoreAlbum ? 100 : Fuzz.Ratio(track.AlbumName.ToLower(), fromTrack.AlbumName.ToLower()),
                TitleMatch = Fuzz.Ratio(track.Title.ToLower(), fromTrack.Title.ToLower())
            })
            .OrderByDescending(track => track.ArtistMatch)
            .ThenByDescending(track => track.AlbumMatch)
            .ThenByDescending(track => track.TitleMatch)
            .ToList();

        var foundTrack = foundTracks
            .Where(track => track.ArtistMatch >= syncConfiguration.MatchPercentage)
            .Where(track => track.AlbumMatch >= syncConfiguration.MatchPercentage)
            .Where(track => track.TitleMatch >= syncConfiguration.MatchPercentage)
            .Where(track => FuzzyHelper.ExactNumberMatch(track.Track.ArtistName, fromTrack.ArtistName))
            .Where(track => ignoreAlbum || FuzzyHelper.ExactNumberMatch(track.Track.AlbumName, fromTrack.AlbumName))
            .Where(track => FuzzyHelper.ExactNumberMatch(track.Track.Title, fromTrack.Title))
            .OrderByDescending(track => track.ArtistMatch)
            .ThenByDescending(track => track.AlbumMatch)
            .ThenByDescending(track => track.TitleMatch)
            .Select(track => track.Track)
            .FirstOrDefault();

        return foundTrack;
    }

    private IProviderService GetProviderServiceTo(SyncConfiguration syncConfiguration, string connectionString)
    {
        switch (syncConfiguration.ToService)
        {
            case "subsonic": return new SubSonicService(connectionString,syncConfiguration.ToSubSonicUsername, syncConfiguration.ToSubSonicPassword, syncConfiguration);
            case "plex": return new PlexService(connectionString, syncConfiguration);
            case "spotify": return new SpotifyService(connectionString, syncConfiguration);
            case "tidal": return new TidalService(connectionString, syncConfiguration);
            case "jellyfin": return new JellyfinService(connectionString, syncConfiguration);
            default:
                throw new System.NotImplementedException(syncConfiguration.ToService);
        }
    }

    private IProviderService GetProviderServiceFrom(SyncConfiguration syncConfiguration, string connectionString)
    {
        switch (syncConfiguration.FromService)
        {
            case "subsonic": return new SubSonicService(connectionString,syncConfiguration.FromSubSonicUsername, syncConfiguration.FromSubSonicPassword, syncConfiguration);
            case "plex": return new PlexService(connectionString, syncConfiguration);
            case "spotify": return new SpotifyService(connectionString, syncConfiguration);
            case "tidal": return new TidalService(connectionString, syncConfiguration);
            case "jellyfin": return new JellyfinService(connectionString, syncConfiguration);
            default:
                throw new System.NotImplementedException(syncConfiguration.FromService);
        }
    }

    private async Task<Guid?> GetLastCompleteTransactionAsync(string serviceName, string name)
    {
        switch (serviceName)
        {
            case "subsonic": 
                return await _snapshotRepository.GetLastCompleteTransactionSubsonicAsync(name);
            case "plex": 
                return await _snapshotRepository.GetLastCompleteTransactionPlexAsync(name);
            case "spotify": 
                return await _snapshotRepository.GetLastCompleteTransactionSpotifyAsync(name);
            case "tidal": 
                return await _snapshotRepository.GetLastCompleteTransactionTidalAsync(name);
            case "jellyfin":
                return await _snapshotRepository.GetLastCompleteTransactionJellyfinAsync(name);
            default:
                throw new System.NotImplementedException(serviceName);
        }
    }
}      