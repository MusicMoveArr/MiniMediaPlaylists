using MiniMediaPlaylists.Models.Navidrome;
using RestSharp;

namespace MiniMediaPlaylists.Services;

public class NavidromeApiService
{
    private LoginResponse? _loginResponse;
    
    public async Task LoginAsync(string serverUrl, string username, string password)
    {
        if (_loginResponse != null)
        {
            return;
        }
        string url = $"{serverUrl}/auth/login";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.AddJsonBody(new LoginRequest
        {
            Username = username,
            Password = password
        });
        _loginResponse = await client.PostAsync<LoginResponse>(request);
    }
    
    public async Task SetPlaylistTrackOrderAsync(string serverUrl, string playlistId, int currentTrackOrder, int newTrackOrder)
    {
        string url = $"{serverUrl}/api/playlist/{playlistId}/tracks/{currentTrackOrder}";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.Method = Method.Put;
        request.AddCookie("X-Nd-Client-Unique-Id", _loginResponse.Id, "/", client.Options.BaseUrl.Host);
        request.AddHeader("X-Nd-Client-Unique-Id", _loginResponse.Id);
        request.AddHeader("Priority", "u=1, i");
        
        request.AddHeader("X-Nd-Authorization", "Bearer " + _loginResponse.Token);
        request.AddJsonBody(new SetPlaylistTrackOrderRequest
        {
            insert_before = newTrackOrder.ToString()
        });
        
        await client.ExecuteAsync(request);
    }
    public async Task<List<PlaylistEntity>?> GetPlaylistsAsync(string serverUrl)
    {
        string url = $"{serverUrl}/api/playlist";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.AddCookie("X-Nd-Client-Unique-Id", _loginResponse.Id, "/", client.Options.BaseUrl.Host);
        request.AddHeader("X-Nd-Client-Unique-Id", _loginResponse.Id);
        request.AddHeader("Priority", "u=1, i");
        request.AddHeader("X-Nd-Authorization", "Bearer " + _loginResponse.Token);
        return await client.GetAsync<List<PlaylistEntity>>(request);
    }
    public async Task<List<TrackEntity>?> GetPlaylistTracksAsync(string serverUrl, string playlistId)
    {
        string url = $"{serverUrl}/api/playlist/{playlistId}/tracks?_order=ASC&_sort=id&playlist_id={playlistId}";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.AddCookie("X-Nd-Client-Unique-Id", _loginResponse.Id, "/", client.Options.BaseUrl.Host);
        request.AddHeader("X-Nd-Client-Unique-Id", _loginResponse.Id);
        request.AddHeader("Priority", "u=1, i");
        request.AddHeader("X-Nd-Authorization", "Bearer " + _loginResponse.Token);
        return await client.GetAsync<List<TrackEntity>>(request);
    }
    public async Task<List<TrackEntity>?> GetStarredTracksAsync(string serverUrl)
    {
        string url = $"{serverUrl}/api/song?&_order=ASC&_sort=title&starred=true";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.AddCookie("X-Nd-Client-Unique-Id", _loginResponse.Id, "/", client.Options.BaseUrl.Host);
        request.AddHeader("X-Nd-Client-Unique-Id", _loginResponse.Id);
        request.AddHeader("Priority", "u=1, i");
        request.AddHeader("X-Nd-Authorization", "Bearer " + _loginResponse.Token);
        return await client.GetAsync<List<TrackEntity>>(request);
    }
    public async Task<List<TrackEntity>?> SearchTrackAsync(string serverUrl, string title, int start, int end)
    {
        string url = $"{serverUrl}/api/song?_start={start}&_end={end}&title={Uri.EscapeDataString(title)}";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.AddCookie("X-Nd-Client-Unique-Id", _loginResponse.Id, "/", client.Options.BaseUrl.Host);
        request.AddHeader("X-Nd-Client-Unique-Id", _loginResponse.Id);
        request.AddHeader("Priority", "u=1, i");
        request.AddHeader("X-Nd-Authorization", "Bearer " + _loginResponse.Token);
        return await client.GetAsync<List<TrackEntity>>(request);
    }
    public async Task AddTrackToPlaylistAsync(string serverUrl, string playlistId, string trackId)
    {
        string url = $"{serverUrl}/api/playlist/{playlistId}/tracks";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.AddCookie("X-Nd-Client-Unique-Id", _loginResponse.Id, "/", client.Options.BaseUrl.Host);
        request.AddHeader("X-Nd-Client-Unique-Id", _loginResponse.Id);
        request.AddHeader("Priority", "u=1, i");
        request.AddHeader("X-Nd-Authorization", "Bearer " + _loginResponse.Token);
        request.AddJsonBody(new
        {
            ids = new string[] { trackId }
        });
        
        await client.ExecutePostAsync(request);
    }
}