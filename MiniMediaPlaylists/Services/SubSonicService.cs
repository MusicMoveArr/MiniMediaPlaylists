using FuzzySharp;
using MiniMediaPlaylists.Helpers;
using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Repositories;
using Spectre.Console;
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
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl, Guid snapshotId)
    {
        return await _subSonicRepository.GetPlaylistsAsync(serverUrl, snapshotId);
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId, Guid snapshotId)
    {
        return await _subSonicRepository.GetPlaylistTracksAsync(serverUrl, playlistId, snapshotId);
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksByNameAsync(string serverUrl, string name, Guid snapshotId)
    {
        return await _subSonicRepository.GetPlaylistTracksByNameAsync(serverUrl, name, snapshotId);
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

        string searchQuery = $"{artist} {title}".Replace("-", string.Empty);
        
        var response = await client.Search.Search3Async(searchQuery, songCount: 200);

        return response.SearchResult.Songs
            .Select(track => new GenericTrack(track.Id, track.Title, track.Artist, track.Album)
            {
                AlbumArtist = track.Path.Split('/').FirstOrDefault()
            })
            .ToList();
    }

    public async Task<List<GenericTrack>> DeepSearchTrackAsync(string serverUrl, string artist, string album, string title, Guid snapshotId)
    {
        List<GenericTrack> trackList = new List<GenericTrack>();
        var connection = new SubsonicConnectionInfo(
            serverUrl: serverUrl,
            username: _username,
            password: _password
        );

        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
        {
            return trackList;
        }
        
        using var client = new SubsonicClient(connection);
        
        //go through the list of artists
        var artistSearchResponse = await client.Search.Search3Async(artist, songCount: 0, albumCount: 0, artistCount: 50);
        for (int index = 50; index < 500; index += 50)
        {
            foreach (var artistResult in artistSearchResponse.SearchResult.Artists
                         .Where(a => Fuzz.PartialRatio(a.Name.ToLower(), artist.ToLower()) >= _syncConfiguration.MatchPercentage)
                         .Where(a => FuzzyHelper.ExactNumberMatch(a.Name, artist)))
            {
                var artistInfo = await client.Browsing.GetArtistAsync(artistResult.Id);

                var albumList = artistInfo.Artist.Album
                    .Where(a => Fuzz.PartialRatio(a.Artist.ToLower(), artist.ToLower()) >= _syncConfiguration.MatchPercentage)
                    .Where(a => string.IsNullOrWhiteSpace(album) || Fuzz.Ratio(a.Name, album) >= _syncConfiguration.MatchPercentage)
                    .Where(a => string.IsNullOrWhiteSpace(album) || FuzzyHelper.ExactNumberMatch(a.Name, album))
                    .ToList();

                foreach (var albumSummary in albumList)
                {
                    var albumInfo = await client.Browsing.GetAlbumAsync(albumSummary.Id);
                
                    var tracks = albumInfo.Album.Song
                        .Where(track => Fuzz.PartialRatio(track.Title.ToLower(), title.ToLower()) >= _syncConfiguration.MatchPercentage)
                        .Where(track => FuzzyHelper.ExactNumberMatch(track.Title, title))
                        .Select(track => new GenericTrack(track.Id, track.Title, track.Artist, track.Album, 0, track.UserRating ?? 0))
                        .ToList();
                    trackList.AddRange(tracks);
                }
            }
            
            artistSearchResponse = await client.Search.Search3Async(artist, songCount: 0, albumCount: 0, artistCount: 50, artistOffset: index);
            if (artistSearchResponse.SearchResult.ArtistCount == 0)
            {
                break;
            }
        }
        
        //go through the list of albums
        if (!string.IsNullOrWhiteSpace(album))
        {
            var albumSearchResponse = await client.Search.Search3Async(album, songCount: 0, albumCount: 50, artistCount: 0);
            for (int index = 50; index < 500; index += 50)
            {
                foreach (var albumResult in albumSearchResponse.SearchResult.Albums
                             .Where(a => Fuzz.PartialRatio(a.Artist.ToLower(), artist.ToLower()) >= _syncConfiguration.MatchPercentage)
                             .Where(a => FuzzyHelper.ExactNumberMatch(a.Name, album))
                             .Where(a => Fuzz.Ratio(a.Name, album) >= _syncConfiguration.MatchPercentage))
                {
                    var albumInfo = await client.Browsing.GetAlbumAsync(albumResult.Id);

                    var tracks = albumInfo.Album.Song
                        .Where(track => Fuzz.PartialRatio(track.Title.ToLower(), title.ToLower()) >= _syncConfiguration.MatchPercentage)
                        .Where(track => FuzzyHelper.ExactNumberMatch(track.Title, title))
                        .Select(track => new GenericTrack(track.Id, track.Title, track.Artist, track.Album, 0, track.UserRating ?? 0))
                        .ToList();
                    trackList.AddRange(tracks);
                }
            
                albumSearchResponse = await client.Search.Search3Async(album, songCount: 0, albumCount: 50, artistCount: 0, albumOffset: index);
                if (albumSearchResponse.SearchResult.AlbumCount == 0)
                {
                    break;
                }
            }
        }
        
        return trackList;
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
        
        return starResponse.IsSuccess;
    }

    public async Task<bool> RateTrackAsync(string serverUrl, GenericTrack track, float rating)
    {
        if (rating > 0)
        {
            var connection = new SubsonicConnectionInfo(
                serverUrl: serverUrl,
                username: _username,
                password: _password
            );
            using var client = new SubsonicClient(connection);
            
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

    public async Task<bool> SetTrackPlaylistOrderAsync(string serverUrl, 
        GenericPlaylist playlist, 
        GenericTrack track, 
        List<GenericTrack> playlistTracks, 
        int newPlaylistOrder)
    {
        return false;
    }
}