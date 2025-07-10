using System.Diagnostics;
using MiniMediaPlaylists.Models.Plex;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaPlaylists.Services;

public class PlexApiService
{
    public PlexApiService()
    {
        
    }

    public async Task<PlexMediaContainerResponse<PlaylistModel>?> GetPlaylistsAsync(string serverUrl, string token)
    {
        string url = $"{serverUrl}/playlists?X-Plex-Token={token}";
        using RestClient client = new RestClient(url);
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        RestRequest request = new RestRequest();

        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await client.GetAsync<PlexMediaContainerResponse<PlaylistModel>>(request);
        });
    }
    public async Task<PlexMediaContainerResponse<PlexTrackModel>?> GetPlaylistTracksAsync(string serverUrl, string token, string ratingKey)
    {
        string url = $"{serverUrl}/playlists/{ratingKey}/items?X-Plex-Token={token}";
        using RestClient client = new RestClient(url);
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<PlexMediaContainerResponse<PlexTrackModel>>(request);
        });
    }
    public async Task<PlexInfoResponse?> GetInfoAsync(string serverUrl, string token)
    {
        string url = $"{serverUrl}/?X-Plex-Token={token}";
        using RestClient client = new RestClient(url);
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<PlexInfoResponse>(request);
        });
    }
    public async Task<PlexMediaContainerResponse<SearchResultEntity<PlexTrackModel>>?> SearchTracksAsync(string serverUrl, string token, string searchTerm)
    {
        string url = $"{serverUrl}/library/search?query={Uri.EscapeDataString(searchTerm)}&searchTypes=music&X-Plex-Token={token}";
        using RestClient client = new RestClient(url);
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<PlexMediaContainerResponse<SearchResultEntity<PlexTrackModel>>>(request);
        });
    }
    public async Task<PlexMediaContainerResponse<PlexTrackModel>?> GetTracksByAlbumRatingKeyAsync(string serverUrl, string token, string albumRatingKey)
    {
        string url = $"{serverUrl}/library/metadata/{albumRatingKey}/children?X-Plex-Token={token}";
        using RestClient client = new RestClient(url);
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<PlexMediaContainerResponse<PlexTrackModel>>(request);
        });
    }
    
    public async Task AddTrackToPlaylistAsync(
        string serverUrl, 
        string token, 
        string playlistRatingKey, 
        string trackRatingKey, 
        string machineIdentifier)
    {
        string url = $"{serverUrl}/playlists/{playlistRatingKey}/items?uri=server://{machineIdentifier}/com.plexapp.plugins.library/library/metadata/{trackRatingKey}&X-Plex-Token={token}";
        using RestClient client = new RestClient(url);
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            await client.PutAsync(request);
        });
    }
    
    public async Task<PlexMediaContainerResponse<PlexPlaylistModel>?> CreatePlaylistAsync(string serverUrl, string token, string title, string trackRatingKey, string machineIdentifier)
    {
        var uri = $"server://{machineIdentifier}/com.plexapp.plugins.library/library/metadata/";
        string url = $"{serverUrl}/playlists?type=audio&title={Uri.EscapeDataString(title)}&smart=0&uri={Uri.EscapeDataString(uri)}&X-Plex-Token={token}";
        using RestClient client = new RestClient(url);
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.PostAsync<PlexMediaContainerResponse<PlexPlaylistModel>>(request);
        });
    }
    
    private AsyncRetryPolicy GetRetryPolicy()
    {
        AsyncRetryPolicy retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(5, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) => {
                    Debug.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                });
        return retryPolicy;
    }
}