using FuzzySharp;
using MiniMediaPlaylists.Helpers;
using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Services;
using Spectre.Console;

namespace MiniMediaPlaylists.Commands;

public class SyncCommandHandler
{
    private readonly string _connectionString;
    private IProviderService _fromProvider;
    private IProviderService _toProvider;
    
    public SyncCommandHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SyncPlaylists(SyncConfiguration syncConfiguration)
    {
        _fromProvider = GetProviderServiceFrom(syncConfiguration, _connectionString);
        _toProvider = GetProviderServiceTo(syncConfiguration, _connectionString);

        var fromPlaylists = await _fromProvider.GetPlaylistsAsync(syncConfiguration.FromName);
        var toPlaylists = await _toProvider.GetPlaylistsAsync(syncConfiguration.ToName);
        
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
                await ParallelHelper.ForEachAsync(fromPlaylists, 1, async fromPlaylist =>
                {
                    var toPlayList = toPlaylists.FirstOrDefault(playlist => 
                        string.Equals(playlist.Name, syncConfiguration.ToPlaylistPrefix + fromPlaylist.Name));

                    bool isLikePlaylist = string.Equals(fromPlaylist.Name, syncConfiguration.LikePlaylistName);
                    if (toPlayList == null)
                    {
                        //create non-existing playlist on "to" service
                        toPlayList = await _toProvider.CreatePlaylistAsync(syncConfiguration.ToName,
                            syncConfiguration.ToPlaylistPrefix + fromPlaylist.Name);
                    }

                    var fromTracks = await _fromProvider.GetPlaylistTracksAsync(syncConfiguration.FromName, fromPlaylist.Id);
                    var toTracks =
                        string.IsNullOrWhiteSpace(toPlayList?.Id) || isLikePlaylist ? [] :
                        await _toProvider.GetPlaylistTracksAsync(syncConfiguration.ToName, toPlayList.Id);

                    var task = ctx.AddTask(Markup.Escape($"Processing Playlist '{fromPlaylist.Name}', 0 of {fromTracks.Count} processed"));
                    task.MaxValue = fromTracks.Count;

                    foreach (var fromTrack in fromTracks)
                    {
                        task.Value++;
                        try
                        {
                            if (!syncConfiguration.ForceAddTrack)
                            {
                                var toTrackExists = toTracks
                                    .Where(track => Fuzz.Ratio(track.ArtistName.ToLower(), fromTrack.ArtistName.ToLower()) >= syncConfiguration.MatchPercentage)
                                    .Where(track => Fuzz.Ratio(track.AlbumName.ToLower(), fromTrack.AlbumName.ToLower()) >= syncConfiguration.MatchPercentage)
                                    .Where(track => Fuzz.Ratio(track.Title.ToLower(), fromTrack.Title.ToLower()) >= syncConfiguration.MatchPercentage)
                                    .Where(track => FuzzyHelper.ExactNumberMatch(track.ArtistName, fromTrack.ArtistName))
                                    .Where(track => FuzzyHelper.ExactNumberMatch(track.AlbumName, fromTrack.AlbumName))
                                    .Any(track => FuzzyHelper.ExactNumberMatch(track.Title, fromTrack.Title));

                                if (toTrackExists)
                                {
                                    //AnsiConsole.WriteLine(Markup.Escape($"Song already in playlist '{fromTrack.ArtistName} - {fromTrack.AlbumName} - {fromTrack.Title}'"));
                                    continue;
                                }
                            }

                            var searchResults = await _toProvider.SearchTrackAsync(
                                syncConfiguration.ToName,
                                fromTrack.ArtistName,
                                fromTrack.AlbumName,
                                fromTrack.Title);

                            var foundTrack = FindTrack(searchResults, syncConfiguration, fromTrack);

                            bool foundWithDeepSearch = false;
                            if (foundTrack == null && syncConfiguration.DeepSearchThroughArtist)
                            {
                                searchResults = await _toProvider.DeepSearchTrackAsync(
                                    syncConfiguration.ToName,
                                    fromTrack.ArtistName,
                                    fromTrack.AlbumName,
                                    fromTrack.Title);
                                foundTrack = FindTrack(searchResults, syncConfiguration, fromTrack);
                                foundWithDeepSearch = foundTrack != null;
                            }

                            if (foundTrack != null)
                            {
                                if (isLikePlaylist)
                                {
                                    if (await _toProvider.LikeTrackAsync(syncConfiguration.ToName, foundTrack, fromTrack.LikeRating))
                                    {
                                        AnsiConsole.WriteLine(Markup.Escape($"Liked song with rating '{fromTrack.LikeRating}' '{fromTrack.ArtistName} - {fromTrack.AlbumName} - {fromTrack.Title}' {(foundWithDeepSearch ? "found with deep search" : "")}"));
                                    }
                                    else
                                    {
                                        AnsiConsole.WriteLine(Markup.Escape($"Failed to like the song, '{fromTrack.ArtistName} - {fromTrack.AlbumName} - {fromTrack.Title}'"));
                                    }
                                }
                                else
                                {
                                    await _toProvider.AddTrackToPlaylistAsync(syncConfiguration.ToName, toPlayList.Id, foundTrack);
                                    AnsiConsole.WriteLine(Markup.Escape($"Added song to playlist '{fromTrack.ArtistName} - {fromTrack.AlbumName} - {fromTrack.Title}' {(foundWithDeepSearch ? "found with deep search" : "")}"));
                                }
                            }
                            else
                            {
                                AnsiConsole.WriteLine(Markup.Escape($"Track not found for '{syncConfiguration.ToService}' '{fromTrack.ArtistName} - {fromTrack.AlbumName} - {fromTrack.Title}'"));
                            }
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Error: {e.Message}"));
                        }
                    }

                    totalProgressTask.Value++;
                    totalProgressTask.Description(Markup.Escape($"Processing Playlists {totalProgressTask.Value} of {fromPlaylists.Count} processed"));
                });
            });
    }

    private GenericTrack? FindTrack(
        List<GenericTrack> searchResults,
        SyncConfiguration syncConfiguration,
        GenericTrack fromTrack)
    {
        var foundTracks = searchResults
            .Select(track => new
            {
                Track = track,
                ArtistMatch = Fuzz.Ratio(track.ArtistName.ToLower(), fromTrack.ArtistName.ToLower()),
                AlbumMatch = Fuzz.Ratio(track.AlbumName.ToLower(), fromTrack.AlbumName.ToLower()),
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
            .Where(track => FuzzyHelper.ExactNumberMatch(track.Track.AlbumName, fromTrack.AlbumName))
            .Where(track => FuzzyHelper.ExactNumberMatch(track.Track.Title, fromTrack.Title))
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
            default:
                throw new System.NotImplementedException(syncConfiguration.FromService);
        }
    }
}      