using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using MiniMediaPlaylists.Models.Jellyfin;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaPlaylists.Services;

public class JellyfinApiService
{
    private AuthenticationResponse _authenticationResponse;

    public async Task<ItemsResponse<T>?> GetItems<T>(string serverUrl, 
        string jellyfinUserId, 
        string accessToken,
        string itemsType,
        bool recursive,
        bool isFavorite)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Jellyfin GetItems");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions($"{serverUrl}/Users/{jellyfinUserId}/Items");
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", GetAuthHeader(accessToken));
            request.AddParameter("SortBy", "SortName");
            request.AddParameter("SortOrder", "Ascending");
            request.AddParameter("IncludeItemTypes", itemsType);
            request.AddParameter("Recursive", recursive);
            request.AddParameter("Fields", "SortName");
            request.AddParameter("StartIndex", "0");
            if (isFavorite)
            {
                request.AddParameter("IsFavorite", "true");
            }
            
            return await client.GetAsync<ItemsResponse<T>>(request);
        });
    }
    public async Task<ItemsResponse<JellyfinTrackItem>?> GetPlaylistTracks(string serverUrl, 
        string jellyfinUserId, 
        string playlistId, 
        string accessToken)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Jellyfin GetPlaylistTracks");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions($"{serverUrl}/Playlists/{playlistId}/Items");
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", GetAuthHeader(accessToken));
            request.AddParameter("UserId", jellyfinUserId);
            return await client.GetAsync<ItemsResponse<JellyfinTrackItem>>(request);
        });
    }
    public async Task AddTrackToPlaylist(string serverUrl, 
        string jellyfinUserId, 
        string playlistId, 
        string trackId, 
        string accessToken)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Jellyfin AddTrackToPlaylist");

        await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions($"{serverUrl}/Playlists/{playlistId}/Items?ids={trackId}&userId={jellyfinUserId}");
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", GetAuthHeader(accessToken));
            
            await client.PostAsync(request);
        });
    }
    public async Task<CreatePlaylistResponse?> CreatePlaylist(string serverUrl, 
        string jellyfinUserId, 
        string name, 
        string accessToken)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Jellyfin CreatePlaylist");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions($"{serverUrl}/Playlists");
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", GetAuthHeader(accessToken));
            request.AddJsonBody(new CreatePlaylistRequest
            {
                Ids = new List<string>(),
                IsPublic = false,
                Name = name,
                UserId = jellyfinUserId
            });
            
            return await client.PostAsync<CreatePlaylistResponse>(request);
        });
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(string serverUrl, string username, string password)
    {
        var client = new RestClient(serverUrl);

        var request = new RestRequest("/Users/AuthenticateByName", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("X-Emby-Authorization", GetAuthHeader(string.Empty));

        var body = new
        {
            Username = username,
            Pw = password
        };

        request.AddJsonBody(body);

        var response = await client.ExecutePostAsync<AuthenticationResponse>(request);

        if (!string.IsNullOrWhiteSpace(response.Data?.User?.Id))
        {
            _authenticationResponse = response.Data;
        }
        
        return response.Data;
    }
    
    public async Task<ItemsResponse<JellyfinTrackItem>?> SearchTracks(string serverUrl, 
        string jellyfinUserId, 
        string accessToken,
        string searchTerm)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Jellyfin SearchTracks");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions($"{serverUrl}/Items");
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", GetAuthHeader(accessToken));
            request.AddParameter("UserId", jellyfinUserId);
            request.AddParameter("Limit", "100");
            request.AddParameter("recursive", "true");
            request.AddParameter("searchTerm", searchTerm);
            request.AddParameter("fields", "MediaSourceCount");
            request.AddParameter("includeItemTypes", "Audio");
            request.AddParameter("enableTotalRecordCount", "false");
            return await client.GetAsync<ItemsResponse<JellyfinTrackItem>>(request);
        });
    }
    
    public async Task<FavoriteTrackResponse?> FavoriteTrack(string serverUrl, 
        string jellyfinUserId, 
        string accessToken,
        string trackId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Jellyfin FavoriteTrack");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions($"{serverUrl}/Users/{jellyfinUserId}/FavoriteItems/{trackId}");
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", GetAuthHeader(accessToken));
            return await client.PostAsync<FavoriteTrackResponse>(request);
        });
    }

    private string GetAuthHeader(string accessToken)
    {
        byte[] hashedDeviceId = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
        string deviceId = BitConverter.ToString(hashedDeviceId).Replace("-", string.Empty);

        StringBuilder sb = new StringBuilder();
        sb.Append("MediaBrowser Client=\"MiniMediaPlaylists\", ");
        sb.Append($"Device=\"{Environment.MachineName}\", ");
        sb.Append($"DeviceId=\"{deviceId}\", ");
        sb.Append("Version=\"1.0.0\"");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            sb.Append($", Token=\"{accessToken}\"");
        }
        return sb.ToString();
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