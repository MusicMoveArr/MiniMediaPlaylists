using FuzzySharp;
using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Repositories;

namespace MiniMediaPlaylists.Services;

public class PlexService : IProviderService
{
    private readonly PlexRepository _plexRepository;
    private readonly PlexApiService _plexApiService;
    private readonly SyncConfiguration _syncConfiguration;
    private Dictionary<string, string> _serverMachineIdentifiers;
    
    public PlexService(string connectionString, SyncConfiguration syncConfiguration)
    {
        _plexRepository = new PlexRepository(connectionString);
        _plexApiService = new PlexApiService();
        _syncConfiguration = syncConfiguration;
        _serverMachineIdentifiers = new Dictionary<string, string>();
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl)
    {
        return await _plexRepository.GetPlaylistsAsync(serverUrl);
    }

    public async Task<GenericPlaylist> CreatePlaylistAsync(string serverUrl, string name)
    {
        string serverMachineIdentifier = await GetMachineIdentifierAsync(serverUrl);
        var response = await _plexApiService.CreatePlaylistAsync(serverUrl, _syncConfiguration.ToPlexToken, name, "", serverMachineIdentifier);
        
        return new GenericPlaylist
        {
            Id = response?.MediaContainer?.Metadata?.FirstOrDefault()?.RatingKey,
            Name = response?.MediaContainer?.Metadata?.FirstOrDefault()?.Title
        };
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId)
    {
        return await _plexRepository.GetPlaylistTracksAsync(serverUrl, playlistId);
    }

    public async Task<List<GenericTrack>> SearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        string searchTerm = $"{artist} {title}";
        var response = await _plexApiService.SearchTracksAsync(serverUrl, _syncConfiguration.ToPlexToken, searchTerm);
        var foundTracks = new List<GenericTrack>();
        
        //go through found albums because Plex does not directly return in the API response of single's
        //But the album of a Single does show up
        foreach (var albumResponse in response?.MediaContainer?.SearchResult
                     ?.Where(track => track.Metadata.Type == "album") ?? [])
        {
            var albumTracks = await _plexApiService.GetChildrenByRatingKeyAsync(serverUrl, 
                _syncConfiguration.ToPlexToken, 
                albumResponse.Metadata.RatingKey);
            
            foundTracks.AddRange(albumTracks.MediaContainer?.Metadata?
                .Where(track => track.Type == "track")
                .Select(track => new GenericTrack
            {
                Id = track.RatingKey,
                AlbumName = track.ParentTitle,
                ArtistName = track.GrandparentTitle,
                Title = track.Title
            }));
        }
        
        
        foundTracks.AddRange(response?.MediaContainer?.SearchResult
            ?.Where(track => track.Metadata.Type == "track")
            ?.Select(track => new GenericTrack
            {
                Id = track.Metadata.RatingKey,
                AlbumName = track.Metadata.ParentTitle,
                ArtistName = track.Metadata.GrandparentTitle,
                Title = track.Metadata.Title
            }) ?? []);
        
        return foundTracks
            .Where(track => !string.IsNullOrWhiteSpace(track.Id))
            .Where(track => !string.IsNullOrWhiteSpace(track.Title))
            .Where(track => !string.IsNullOrWhiteSpace(track.AlbumName))
            .Where(track => !string.IsNullOrWhiteSpace(track.ArtistName))
            .DistinctBy(track => track.Id)
            .ToList();
    }

    public async Task<List<GenericTrack>> DeepSearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        string serverMachineIdentifier = await GetMachineIdentifierAsync(serverUrl);
        var response = await _plexApiService.SearchTracksAsync(serverUrl, _syncConfiguration.ToPlexToken, artist);
        var foundTracks = new List<GenericTrack>();

        List<int> librarySectionIds = await _plexRepository.GetLibrarySectionIdsAsync(serverUrl);

        foreach (var searchResult in response.MediaContainer.SearchResult
                     .Where(a => a.Metadata.Type == "artist")
                     .Where(a => Fuzz.Ratio(a.Metadata.Title, artist) >= _syncConfiguration.MatchPercentage))
        {
            //process Albums
            var albumsSearchResult = await _plexApiService.GetChildrenByRatingKeyAsync(serverUrl, 
                _syncConfiguration.ToPlexToken, 
                searchResult.Metadata.RatingKey);
            
            var albums = albumsSearchResult.MediaContainer?.Metadata?
                .Where(album => album.Type == "album")
                .Where(a => Fuzz.Ratio(a.Title.ToLower(), album.ToLower()) >= _syncConfiguration.MatchPercentage)
                .ToList();

            foreach (var foundAlbum in albums ?? [])
            {
                var tracks = await _plexApiService.GetChildrenByRatingKeyAsync(serverUrl, 
                    _syncConfiguration.ToPlexToken, 
                    foundAlbum.RatingKey);
                
                foundTracks.AddRange(tracks.MediaContainer?.Metadata?
                    .Where(track => track.Type == "track")
                    .Select(track => new GenericTrack
                    {
                        Id = track.RatingKey,
                        AlbumName = track.ParentTitle,
                        ArtistName = track.GrandparentTitle,
                        Title = track.Title
                    })
                    .ToList());
            }
            
            //process Singles & EPs
            foreach (var sectionId in librarySectionIds)
            {
                var singleEPsSearchResult = await _plexApiService
                    .GetSingleEPsByArtistRatingKeyAsync(serverUrl, 
                    _syncConfiguration.ToPlexToken,
                    searchResult.Metadata.RatingKey,
                    sectionId);

                foreach (var singleEp in singleEPsSearchResult
                             ?.MediaContainer?.Metadata
                             ?.Where(single => single.Type == "album")
                             ?.Where(single => Fuzz.Ratio(single.Title.ToLower(), album.ToLower()) >= _syncConfiguration.MatchPercentage) ?? [])
                {
                    var tracks = await _plexApiService.GetChildrenByRatingKeyAsync(serverUrl, 
                        _syncConfiguration.ToPlexToken, 
                        singleEp.RatingKey);
                
                    foundTracks.AddRange(tracks.MediaContainer?.Metadata?
                        .Where(track => track.Type == "track")
                        .Select(track => new GenericTrack
                        {
                            Id = track.RatingKey,
                            AlbumName = track.ParentTitle,
                            ArtistName = track.GrandparentTitle,
                            Title = track.Title
                        })
                        .ToList());
                }
            }
        }
        
        return foundTracks
            .Where(track => !string.IsNullOrWhiteSpace(track.Id))
            .Where(track => !string.IsNullOrWhiteSpace(track.Title))
            .Where(track => !string.IsNullOrWhiteSpace(track.AlbumName))
            .Where(track => !string.IsNullOrWhiteSpace(track.ArtistName))
            .DistinctBy(track => track.Id)
            .ToList();
    }

    public async Task<bool> AddTrackToPlaylistAsync(string serverUrl, string playlistId, GenericTrack track)
    {
        string serverMachineIdentifier = await GetMachineIdentifierAsync(serverUrl);
        await _plexApiService.AddTrackToPlaylistAsync(serverUrl, _syncConfiguration.ToPlexToken, playlistId, track.Id, serverMachineIdentifier);
        return true;
    }

    private async Task<string> GetMachineIdentifierAsync(string serverUrl)
    {
        if (!_serverMachineIdentifiers.ContainsKey(serverUrl))
        {
            var serverInfo = await _plexApiService.GetInfoAsync(serverUrl, _syncConfiguration.ToPlexToken);
            if (!string.IsNullOrWhiteSpace(serverInfo?.MediaContainer?.MachineIdentifier))
            {
                _serverMachineIdentifiers.Add(serverUrl, serverInfo.MediaContainer.MachineIdentifier);
            }
        }
        string serverMachineIdentifier = _serverMachineIdentifiers[serverUrl];
        return serverMachineIdentifier;
    }

    public async Task<bool> LikeTrackAsync(string serverUrl, GenericTrack track, float rating)
    {
        if (rating <= 0)
        {
            return false;
        }
        
        rating = _syncConfiguration.FromService switch
        {
            SyncConfiguration.ServicePlex => rating,
            SyncConfiguration.ServiceSubsonic => rating * 2,
            _ => rating
        };

        await _plexApiService.LikeTrackAsync(serverUrl, _syncConfiguration.ToPlexToken, track.Id, (int)rating);
        
        return true;
    }
}