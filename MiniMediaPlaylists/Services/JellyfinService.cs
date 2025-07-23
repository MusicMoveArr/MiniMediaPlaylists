using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Repositories;

namespace MiniMediaPlaylists.Services;

public class JellyfinService : IProviderService
{
    private readonly JellyfinRepository _jellyfinRepository;
    private readonly JellyfinApiService _jellyfinApiService;
    private readonly SyncConfiguration _syncConfiguration;

    public JellyfinService(string connectionString, SyncConfiguration syncConfiguration)
    {
        _jellyfinRepository = new JellyfinRepository(connectionString);
        _jellyfinApiService = new JellyfinApiService();
        _syncConfiguration = syncConfiguration;
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl)
    {
        return await _jellyfinRepository.GetPlaylistsAsync(serverUrl);
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId)
    {
        return await _jellyfinRepository.GetPlaylistTracksAsync(serverUrl, playlistId);
    }

    public async Task<GenericPlaylist> CreatePlaylistAsync(string serverUrl, string name)
    {
        var dbAuthInfo = await _jellyfinRepository.GetOwnerByNameAsync(_syncConfiguration.ToJellyfinUsername, serverUrl);
        var response = await _jellyfinApiService.CreatePlaylist(serverUrl, dbAuthInfo.JellyfinUserId, name, dbAuthInfo.AccessToken);
        return new GenericPlaylist
        {
            Id = response.Id,
            Name = name,
        };
    }

    public async Task<List<GenericTrack>> SearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        var dbAuthInfo = await _jellyfinRepository.GetOwnerByNameAsync(_syncConfiguration.ToJellyfinUsername, serverUrl);

        var tracks = await _jellyfinApiService
            .SearchTracks(serverUrl, dbAuthInfo.JellyfinUserId, dbAuthInfo.AccessToken, title);


        return tracks.Items.Select(track => new GenericTrack
        {
            Id = track.Id,
            AlbumName = track.Album,
            ArtistName = track.AlbumArtist,
            LikeRating = 0,
            Title = track.Name,
            Uri = string.Empty
        }).ToList();
    }

    public async Task<List<GenericTrack>> DeepSearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        return new List<GenericTrack>();
    }

    public async Task<bool> AddTrackToPlaylistAsync(string serverUrl, string playlistId, GenericTrack track)
    {
        var dbAuthInfo = await _jellyfinRepository.GetOwnerByNameAsync(_syncConfiguration.ToJellyfinUsername, serverUrl);
        await _jellyfinApiService.AddTrackToPlaylist(_syncConfiguration.ToName, dbAuthInfo.JellyfinUserId, playlistId, track.Id, dbAuthInfo.AccessToken);

        return true;
    }

    public async Task<bool> LikeTrackAsync(string serverUrl, GenericTrack track, float rating)
    {
        var dbAuthInfo = await _jellyfinRepository.GetOwnerByNameAsync(_syncConfiguration.ToJellyfinUsername, serverUrl);
        var response = await _jellyfinApiService.FavoriteTrack(_syncConfiguration.ToName, dbAuthInfo.JellyfinUserId, dbAuthInfo.AccessToken, track.Id);
        return response?.IsFavorite == true;
    }
}