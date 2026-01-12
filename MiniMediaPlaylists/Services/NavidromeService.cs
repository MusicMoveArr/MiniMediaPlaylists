using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Models.Navidrome;
using MiniMediaPlaylists.Repositories;
using RestSharp;
using SubSonicMedia;
using SubSonicMedia.Models;

namespace MiniMediaPlaylists.Services;

public class NavidromeService : IProviderService
{
    private readonly SubSonicRepository _subsonicRepository;
    private readonly NavidromeApiService _navidromeApiService;
    private readonly SyncConfiguration _syncConfiguration;
    private readonly string _username;
    private readonly string _password;
    
    public NavidromeService(string connectionString, string username, string password, SyncConfiguration syncConfiguration)
    {
        _subsonicRepository = new SubSonicRepository(connectionString);
        _navidromeApiService = new NavidromeApiService();
        _username = username;
        _password = password;
        _syncConfiguration = syncConfiguration;
    }

    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl, Guid snapshotId)
    {
        return await _subsonicRepository.GetPlaylistsAsync(serverUrl, snapshotId);
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId, Guid snapshotId)
    {
        return await _subsonicRepository.GetPlaylistTracksAsync(serverUrl, playlistId, snapshotId);
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksByNameAsync(string serverUrl, string name, Guid snapshotId)
    {
        return await _subsonicRepository.GetPlaylistTracksByNameAsync(serverUrl, name, snapshotId);
    }

    public async Task<GenericPlaylist> CreatePlaylistAsync(string serverUrl, string name)
    {
        using var client = GetSubsonicClient(serverUrl);
        var response = await client.Playlists.CreatePlaylistAsync(name);
        
        return new GenericPlaylist
        {
            Id = response.Playlist.Id,
            Name = response.Playlist.Name,
        };
    }

    public async Task<List<GenericTrack>> SearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        await _navidromeApiService.LoginAsync(serverUrl, _username, _password);

        var result = new List<GenericTrack>();
        int pages = 10;
        int chunksize = 1000;
        int start = 0;
        int end = 1000;

        for (int page = 0; page < pages; page++)
        {
            var tracks = await _navidromeApiService.SearchTrackAsync(serverUrl, $"{artist} {title}", start, end);

            foreach (var track in tracks?.Where(track => !track.Missing) ?? [])
            {
                List<string> artists = new List<string>();
                artists.Add(track.Artist);
                artists.Add(track.AlbumArtist);
                artists.AddRange(track.Participants.AlbumArtist
                    .Select(a => a.Name));
            
                artists.AddRange(track.Participants.Artist
                    .Select(a => a.Name));
            
                result.AddRange(artists.Distinct().Select(a => 
                    new GenericTrack(track.Id, track.Title, a, track.Album)
                    {
                        AlbumArtist = track.AlbumArtist,
                        LikeRating = track.Rating
                    }));
            }

            start += chunksize;
            end += chunksize;

            if (tracks?.Count != chunksize)
            {
                break;
            }
        }

        return result;
    }

    public async Task<List<GenericTrack>> DeepSearchTrackAsync(string serverUrl, string artist, string album, string title, Guid snapshotId)
    {
        return new List<GenericTrack>();
    }

    public async Task<bool> AddTrackToPlaylistAsync(string serverUrl, string playlistId, GenericTrack track)
    {
        await _navidromeApiService.AddTrackToPlaylistAsync(serverUrl, playlistId, track.Id);
        return true;
    }

    public async Task<bool> LikeTrackAsync(string serverUrl, GenericTrack track, float rating)
    {
        using var client = GetSubsonicClient(serverUrl);
        var starResponse = await client.Annotation.StarAsync(track.Id);
        return starResponse.IsSuccess;
    }

    public async Task<bool> RateTrackAsync(string serverUrl, GenericTrack track, float rating)
    {
        if (rating > 0)
        {
            using var client = GetSubsonicClient(serverUrl);
            rating = _syncConfiguration.FromService switch
            {
                SyncConfiguration.ServicePlex => rating / 2F,
                SyncConfiguration.ServiceSubsonic => rating,
                _ => rating
            };
            await client.Annotation.SetRatingAsync(track.Id, (int)rating);
        }
        return false;
    }

    public async Task<bool> SetTrackPlaylistOrderAsync(string serverUrl, GenericPlaylist playlist, GenericTrack track, List<GenericTrack> playlistTracks,
        int newPlaylistOrder)
    {
        await _navidromeApiService.LoginAsync(serverUrl, _username, _password);
        await _navidromeApiService.SetPlaylistTrackOrderAsync(serverUrl, playlist.Id, track.PlaylistSortOrder, newPlaylistOrder);
        return true;
    }

    private SubsonicClient GetSubsonicClient(string serverUrl) =>
        new SubsonicClient(new SubsonicConnectionInfo(
            serverUrl: serverUrl,
            username: _username,
            password: _password
        ));
}