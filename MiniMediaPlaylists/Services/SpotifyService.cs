using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Repositories;
using SpotifyAPI.Web;

namespace MiniMediaPlaylists.Services;

public class SpotifyService : IProviderService
{
    private readonly SpotifyRepository _spotifyRepository;
    private SpotifyClient _spotifyClient;
    
    public SpotifyService(string connectionString)
    {
        _spotifyRepository = new SpotifyRepository(connectionString);
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string ownerId)
    {
        return await _spotifyRepository.GetPlaylistsAsync(ownerId);
    }

    public async Task<GenericPlaylist> CreatePlaylistAsync(string serverUrl, string name)
    {
        if (_spotifyClient == null)
        {
            _spotifyClient = await GetSpotifyClientAync(serverUrl);
        }

        var playlist = await _spotifyClient.Playlists.Create(serverUrl, new PlaylistCreateRequest(name));

        return new GenericPlaylist
        {
            Id = playlist.Id,
            Name = playlist.Name
        };
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId)
    {
        return await _spotifyRepository.GetPlaylistTracksAsync(serverUrl, playlistId);
    }

    public async Task<List<GenericTrack>> SearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        if (_spotifyClient == null)
        {
            _spotifyClient = await GetSpotifyClientAync(serverUrl);
        }

        SearchRequest request = new SearchRequest(SearchRequest.Types.Track, $"{artist} {title}");
        var searchResponse = await _spotifyClient.Search.Item(request);
        var tracks = searchResponse.Tracks.Items
            .Where(track => track.Type == ItemType.Track)
            .Select(track => 
                new GenericTrack
            {
                Id = track.Uri,
                AlbumName = track.Album.Name,
                ArtistName = track.Artists.FirstOrDefault().Name,
                Title = track.Name
            })
            .Where(track => !string.IsNullOrWhiteSpace(track.Id))
            .Where(track => !string.IsNullOrWhiteSpace(track.AlbumName))
            .Where(track => !string.IsNullOrWhiteSpace(track.ArtistName))
            .Where(track => !string.IsNullOrWhiteSpace(track.Title))
            .ToList();
        
        return tracks;
    }

    public async Task<bool> AddTrackToPlaylistAsync(string serverUrl, string playlistId, string trackId)
    {
        if (_spotifyClient == null)
        {
            _spotifyClient = await GetSpotifyClientAync(serverUrl);
        }

        var request = new PlaylistAddItemsRequest([trackId]);
        await _spotifyClient.Playlists.AddItems(playlistId, request);
        return true;
    }

    private async Task<SpotifyClient> GetSpotifyClientAync(string ownerName)
    {
        var spotifyOwnerModel = await _spotifyRepository.GetOwnerByNameAsync(ownerName);
        var refreshRequest = new AuthorizationCodeRefreshRequest(spotifyOwnerModel.AuthClientId,
            spotifyOwnerModel.AuthSecretId, spotifyOwnerModel.AuthRefreshToken);
        var newToken = await new OAuthClient().RequestToken(refreshRequest);
        return new SpotifyClient(newToken.AccessToken);
    }
}