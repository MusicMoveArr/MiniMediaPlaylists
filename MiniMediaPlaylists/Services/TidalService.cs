using FuzzySharp;
using MiniMediaPlaylists.Helpers;
using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Models.Tidal;
using MiniMediaPlaylists.Repositories;

namespace MiniMediaPlaylists.Services;

public class TidalService : IProviderService
{
    private readonly TidalRepository _tidalRepository;
    private readonly SyncConfiguration _syncConfiguration;
    private TidalAPIService? _tidalApiService;

    public TidalService(string connectionString, SyncConfiguration syncConfiguration)
    {
        _tidalRepository = new TidalRepository(connectionString);
        _syncConfiguration = syncConfiguration;
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl, Guid snapshotId)
    {
        return await _tidalRepository.GetPlaylistsAsync(serverUrl, snapshotId);
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId, Guid snapshotId)
    {
        return await _tidalRepository.GetPlaylistTracksAsync(serverUrl, playlistId, snapshotId);
    }

    public async Task<List<GenericTrack>> GetPlaylistTracksByNameAsync(string serverUrl, string name, Guid snapshotId)
    {
        return await _tidalRepository.GetPlaylistTracksByNameAsync(serverUrl, name, snapshotId);
    }

    public async Task<GenericPlaylist> CreatePlaylistAsync(string serverUrl, string name)
    {
        await AuthenticateAsync();
        var response = await _tidalApiService.CreatePlaylistAsync(name);
        return new GenericPlaylist
        {
            Id = response.Data.Id,
            Name = name
        };
    }

    public async Task<List<GenericTrack>> SearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        await AuthenticateAsync();
        
        var searchResult = await _tidalApiService.SearchResultsTracksAsync($"{artist} {title}");
        searchResult = await GetAllTracksFromSearchAsync(searchResult);
        List<GenericTrack> foundTracks = new List<GenericTrack>();

        if (searchResult?.Included != null)
        {
            List<TidalSearchDataEntity> bestTrackMatches = FindBestMatchingTracks(searchResult?.Included, title);
            bestTrackMatches = bestTrackMatches
                .Where(track => !string.IsNullOrWhiteSpace(track.RelationShips.Albums.Links.Self))
                .DistinctBy(track => new
                {
                    track.Id,
                    track.RelationShips.Albums.Links.Self
                })
                .ToList();

            foreach (var result in bestTrackMatches)
            {
                var artistNames = await GetTrackArtistsAsync(int.Parse(result.Id));
                bool containsArtist = artistNames.Any(artistName => 
                                          Fuzz.TokenSortRatio(artist.ToLower(), artistName.ToLower()) > _syncConfiguration.MatchPercentage) ||
                                          Fuzz.TokenSortRatio(artist.ToLower(), string.Join(' ', artistNames).ToLower()) > _syncConfiguration.MatchPercentage; //maybe collab?

                if (!containsArtist)
                {
                    continue;
                }
                var tidalAlbum = await _tidalApiService.GetAlbumSelfInfoAsync(result.RelationShips.Albums.Links.Self);
                var albumIds = tidalAlbum.Data
                    .Where(a => a.Type == "albums")
                    .Select(a => int.Parse(a.Id))
                    .Distinct()
                    .ToList();
                
                foreach (var albumId in albumIds)
                {
                    var albumTracks = await GetAllTracksByAlbumIdAsync(albumId);

                    if (albumTracks?.Included == null)
                    {
                        continue;
                    }

                    if (Fuzz.Ratio(album.ToLower(), albumTracks.Data.Attributes.Title.ToLower()) < _syncConfiguration.MatchPercentage ||
                         !FuzzyHelper.ExactNumberMatch(album.ToLower(), albumTracks?.Data.Attributes.Title.ToLower()))
                    {
                        continue;
                    }

                    var trackMatches = FindBestMatchingTracks(albumTracks.Included, title);

                    foreach (var trackMatch in trackMatches)
                    {
                        foundTracks.Add(new GenericTrack(
                            trackMatch.Id, 
                            trackMatch.Attributes.Title,
                            artistNames.FirstOrDefault(),
                            albumTracks.Data.Attributes.Title
                            ));
                    }
                }
            }
        }
        
        return foundTracks;
    }

    public async Task<List<GenericTrack>> DeepSearchTrackAsync(string serverUrl, string artist, string album, string title)
    {
        return new List<GenericTrack>();
    }

    public async Task<bool> AddTrackToPlaylistAsync(string serverUrl, string playlistId, GenericTrack track)
    {
        await AuthenticateAsync();
        
        await _tidalApiService.AddTrackToPlaylistAsync(playlistId, track.Id);
        return false;
    }

    public async Task<bool> LikeTrackAsync(string serverUrl, GenericTrack track, float rating)
    {
        return false;
    }

    public async Task<bool> RateTrackAsync(string serverUrl, GenericTrack track, float rating)
    {
        return false;
    }

    private async Task AuthenticateAsync()
    {
        if (_tidalApiService == null)
        {
            var owner = await _tidalRepository.GetOwnerByNameAsync(_syncConfiguration.ToName);
            _tidalApiService = new TidalAPIService(_syncConfiguration.ToTidalCountryCode, owner.AuthClientId);
            await _tidalApiService.AuthenticateWithRefreshTokenAsync(owner.AuthRefreshToken);
        }
    }
    
    private async Task<TidalSearchResponse?> GetAllTracksFromSearchAsync(TidalSearchResponse searchResults)
    {
        if (searchResults?.Included?.Count >= 20)
        {
            string? nextPage = searchResults.Data.RelationShips?.Tracks?.Links?.Next;
            while (!string.IsNullOrWhiteSpace(nextPage))
            {
                var tempTracks = await _tidalApiService.GetTracksNextFromSearchAsync(nextPage);

                if (tempTracks?.Included?.Count > 0)
                {
                    searchResults.Included.AddRange(tempTracks.Included);
                }

                if (tempTracks?.Data?.Count > 0)
                {
                    searchResults.Data
                        ?.RelationShips
                        ?.Items
                        ?.Data
                        ?.AddRange(tempTracks.Data);
                }
                nextPage = tempTracks?.Links?.Next;
            }
        }
        
        return searchResults;
    }

    private List<TidalSearchDataEntity> FindBestMatchingTracks(
        List<TidalSearchDataEntity> searchResults, 
        string targetTrackTitle)
    {
        //strict name matching
        return searchResults
            ?.Where(t => t.Type == "tracks")
            ?.Select(t => new
            {
                TitleMatchedFor = Fuzz.Ratio(targetTrackTitle?.ToLower(), t.Attributes.FullTrackName.ToLower()),
                Track = t
            })
            .Where(match => FuzzyHelper.ExactNumberMatch(targetTrackTitle, match.Track.Attributes.FullTrackName))
            .Where(match => match.TitleMatchedFor >= _syncConfiguration.MatchPercentage)
            .OrderByDescending(result => result.TitleMatchedFor)
            .Select(result => result.Track)
            .ToList() ?? [];
    }

    private async Task<List<string>> GetTrackArtistsAsync(int trackId, string primaryArtistName = "", bool onlyAssociated = false)
    {
        if (trackId == 0)
        {
            return new List<string>();
        }
        
        var trackArtists = await _tidalApiService.GetTrackArtistsByTrackIdAsync([trackId]);

        if (trackArtists?.Included == null)
        {
            return new List<string>();
        }

        var artistNames = trackArtists.Included
            .Where(artistName => !string.IsNullOrWhiteSpace(artistName.Attributes.Name))
            .Select(artistName => artistName.Attributes.Name)
            .ToList()!;

        if (onlyAssociated)
        {
            artistNames = artistNames
                .Where(artistName => !string.Equals(artistName, primaryArtistName))
                .ToList();
        }
        return artistNames;
    }
    private async Task<TidalSearchResponse?> GetAllTracksByAlbumIdAsync(int albumId)
    {
        var tracks = await _tidalApiService.GetTracksByAlbumIdAsync(albumId);
        
        if (tracks?.Data.Attributes.NumberOfItems >= 20)
        {
            string? nextPage = tracks.Data.RelationShips?.Items?.Links?.Next;
            while (!string.IsNullOrWhiteSpace(nextPage))
            {
                var tempTracks = await _tidalApiService.GetTracksNextByAlbumIdAsync(albumId, nextPage);

                if (tempTracks?.Included?.Count > 0)
                {
                    tracks.Included.AddRange(tempTracks.Included);
                }

                if (tempTracks?.Data?.Count > 0)
                {
                    tracks.Data
                        ?.RelationShips
                        ?.Items
                        ?.Data
                        ?.AddRange(tempTracks.Data);
                }
                nextPage = tempTracks?.Links?.Next;
            }
        }
        return tracks;
    }
    public async Task<bool> SetTrackPlaylistOrderAsync(string serverUrl, GenericPlaylist playlist, GenericTrack track, List<GenericTrack> playlistTracks, int newPlaylistOrder)
    {
        return false;
    }
}