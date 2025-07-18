using MiniMediaPlaylists.Commands;
using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Repositories;
using SubSonicMedia;
using SubSonicMedia.Models;

namespace MiniMediaPlaylists.Services;

public class SubSonicService : IProviderService
{
    private readonly SubSonicRepository _subSonicRepository;
    private readonly SyncConfiguration _syncConfiguration;
    private readonly string _username;
    private readonly string _password;

    public SubSonicService(string connectionString, string username, string password, SyncConfiguration syncConfiguration)
    {
        _subSonicRepository = new SubSonicRepository(connectionString);
        _syncConfiguration = syncConfiguration;
        _username = username;
        _password = password;
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl)
    {
        return await _subSonicRepository.GetPlaylistsAsync(serverUrl);
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId)
    {
        return await _subSonicRepository.GetPlaylistTracksAsync(serverUrl, playlistId);
    }

    public async Task<GenericPlaylist> CreatePlaylistAsync(string serverUrl, string name)
    {
        var connection = new SubsonicConnectionInfo(
            serverUrl: serverUrl,
            username: _username,
            password: _password
        );
        using var client = new SubsonicClient(connection);
        var response = await client.Playlists.CreatePlaylistAsync(name);
        
        return new GenericPlaylist
        {
            Id = response.Playlist.Id,
            Name = response.Playlist.Name,
        };
    }

    public async Task<List<GenericTrack>> SearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        var connection = new SubsonicConnectionInfo(
            serverUrl: serverUrl,
            username: _username,
            password: _password
        );
        using var client = new SubsonicClient(connection);

        string searchQuery = $"{artist} {title}";
        var response = await client.Search.Search3Async(searchQuery);

        return response.SearchResult.Songs.Select(track => new GenericTrack
        {
            Id = track.Id,
            AlbumName = track.Album,
            ArtistName = track.Artist,
            Title = track.Title,
        }).ToList();
    }

    public async Task<List<GenericTrack>> DeepSearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        return new List<GenericTrack>();
    }

    public async Task<bool> AddTrackToPlaylistAsync(string serverUrl, string playlistId, GenericTrack track)
    {
        var connection = new SubsonicConnectionInfo(
            serverUrl: serverUrl,
            username: _username,
            password: _password
        );
        using var client = new SubsonicClient(connection);

        await client.Playlists.UpdatePlaylistAsync(playlistId, songIdsToAdd: [track.Id]);

        return true;
    }

    public async Task<bool> LikeTrackAsync(string serverUrl, GenericTrack track, float rating)
    {
        var connection = new SubsonicConnectionInfo(
            serverUrl: serverUrl,
            username: _username,
            password: _password
        );
        using var client = new SubsonicClient(connection);

        var starResponse = await client.Annotation.StarAsync(track.Id);
        if (rating > 0)
        {
            rating = _syncConfiguration.FromService switch
            {
                SyncConfiguration.ServicePlex => rating / 2F,
                SyncConfiguration.ServiceSubsonic => rating,
                _ => rating
            };
            await client.Annotation.SetRatingAsync(track.Id, (int)rating);
        }
        
        return starResponse.IsSuccess;
    }
}