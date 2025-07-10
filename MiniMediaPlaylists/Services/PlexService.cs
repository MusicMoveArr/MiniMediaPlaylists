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
        string searchTerm = $"{artist} - {title}";
        var response = await _plexApiService.SearchTracksAsync(serverUrl, _syncConfiguration.ToPlexToken, searchTerm);
        var foundTracks = new List<GenericTrack>();
        
        //go through found albums because Plex does not directly return in the API response of single's
        //But the album of a Single does show up
        foreach (var albumResponse in response?.MediaContainer?.SearchResult
                     ?.Where(track => track.Metadata.Type == "album") ?? [])
        {
            var albumTracks = await _plexApiService.GetTracksByAlbumRatingKeyAsync(serverUrl, 
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

    public async Task<bool> AddTrackToPlaylistAsync(string serverUrl, string playlistId, string trackId)
    {
        string serverMachineIdentifier = await GetMachineIdentifierAsync(serverUrl);
        await _plexApiService.AddTrackToPlaylistAsync(serverUrl, _syncConfiguration.ToPlexToken, playlistId, trackId, serverMachineIdentifier);
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
}