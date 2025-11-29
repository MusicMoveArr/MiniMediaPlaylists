using MiniMediaPlaylists.Models.Navidrome;
using RestSharp;

namespace MiniMediaPlaylists.Services;

public class NavidromeService
{
    private readonly string _serverUrl;
    private readonly string _username;
    private readonly string _password;
    public LoginResponse? LoginResponse { get; set; }
    
    public NavidromeService(string serverUrl, string username, string password)
    {
        _serverUrl = serverUrl;
        _username = username;
        _password = password;
    }
    
    public async Task LoginAsync()
    {
        string url = $"{_serverUrl}/auth/login";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.AddJsonBody(new LoginRequest
        {
            Username = _username,
            Password = _password
        });
        
        this.LoginResponse = await client.PostAsync<LoginResponse>(request);
    }
    
    public async Task SetPlaylistTrackOrderAsync(string playlistId, int currentTrackOrder, int newTrackOrder)
    {
        string url = $"{_serverUrl}/api/playlist/{playlistId}/tracks/{currentTrackOrder}";
        using RestClient client = new RestClient(url);
        
        RestRequest request = new RestRequest();
        request.Method = Method.Put;
        request.AddCookie("X-Nd-Client-Unique-Id", this.LoginResponse.Id, "/", client.Options.BaseUrl.Host);
        request.AddHeader("X-Nd-Client-Unique-Id", this.LoginResponse.Id);
        request.AddHeader("Priority", "u=1, i");
        
        request.AddHeader("X-Nd-Authorization", "Bearer " + this.LoginResponse.Token);
        request.AddJsonBody(new SetPlaylistTrackOrderRequest
        {
            insert_before = newTrackOrder.ToString()
        });
        
        await client.ExecuteAsync(request);
    }
}