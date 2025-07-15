using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using MiniMediaPlaylists.Models.Tidal;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaPlaylists.Services;

public class TidalAPIService
{
    private const string AuthTokenUrl = "https://auth.tidal.com/v1/oauth2/token";
    private const string OAuthAuthorizeUrl = "https://auth.tidal.com/oauth2/authorize";
    private const string SearchResultArtistsUrl = "https://openapi.tidal.com/v2/searchResults/";
    private const string ArtistsIdUrl = "https://openapi.tidal.com/v2/artists/{0}";
    private const string TracksByAlbumIdUrl = "https://openapi.tidal.com/v2/albums/{0}";
    private const string TracksUrl = "https://openapi.tidal.com/v2/tracks";
    private const string TidalApiPrefix = "https://openapi.tidal.com/v2";
    private const string PlaylistsUrl = "https://openapi.tidal.com/v2/playlists";
    private const string GetPlaylistByIdUrl = "https://openapi.tidal.com/v2/playlists/{0}";
    private const string GetTrackByIdUrl = "https://openapi.tidal.com/v2/tracks/{0}";
    private const string SearchResultsUrl = "https://openapi.tidal.com/v2/searchResults/{0}";
    private const string PlaylistPostItemsUrl = "https://openapi.tidal.com/v2/playlists/{0}/relationships/items";
    private const string UsersMeUrl = "https://openapi.tidal.com/v2/users/me";

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _countryCode;
    private string _clientUniqueKey;
    public string CodeVerifier { get; private set; }
    public string CodeChallenge { get; private set; }

    public TidalAuthenticationResponse? AuthenticationResponse { get; private set; }

    public TidalAPIService(string clientId, 
        string clientSecret, 
        string countryCode)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _countryCode = countryCode;
    }
    public TidalAPIService(string countryCode, string clientId)
    {
        _countryCode = countryCode;
        _clientId = clientId;
    }
    
    public async Task<CurrentUserResponse?> GetCurrentUserAsync()
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetCurrentUser");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(UsersMeUrl);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            
            return await client.GetAsync<CurrentUserResponse>(request);
        });
    }
    
    public async Task<PlaylistResponse?> GetPlaylistsAsync(string userId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetPlaylists");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(PlaylistsUrl);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("countryCode", _countryCode);
            request.AddParameter("filter[r.owners.id]", userId);
            
            return await client.GetAsync<PlaylistResponse>(request);
        });
    }
    public async Task<PlaylistResponse?> GetPlaylistsNextAsync(string next)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetPlaylistsNext");
        
        string url = $"{TidalApiPrefix}{next}";

        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            
            return await client.GetAsync<PlaylistResponse>(request);
        });
    }
    
    public async Task<PlaylistByIdResponse?> GetPlaylistByIdAsync(string playlistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetPlaylistById");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(GetPlaylistByIdUrl, playlistId));
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("countryCode", _countryCode);
            request.AddParameter("include", "items,owners");
            
            return await client.GetAsync<PlaylistByIdResponse>(request);
        });
    }
    public async Task<PlaylistByIdNextResponse?> GetPlaylistByIdNextAsync(string next)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetPlaylistByIdNext");
        
        string url = $"{TidalApiPrefix}{next}";

        if (!url.Contains("include="))
        {
            url += "&include=items,owners";
        }
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            
            return await client.GetAsync<PlaylistByIdNextResponse>(request);
        });
    }
    
    public async Task<TrackByIdResponse?> GetTrackByIdAsync(string trackId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetTrackById");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(GetTrackByIdUrl, trackId));
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("countryCode", _countryCode);
            request.AddParameter("include", "albums,artists");
            
            return await client.GetAsync<TrackByIdResponse>(request);
        });
    }
    
    public string GetPkceLoginUrl(string authRedirectUri)
    {
        _clientUniqueKey = $"{BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0):x}";
        this.CodeVerifier = ToBase64UrlEncoded(RandomNumberGenerator.GetBytes(32));

        using var sha256 = SHA256.Create();
        this.CodeChallenge = ToBase64UrlEncoded(sha256.ComputeHash(Encoding.UTF8.GetBytes(this.CodeVerifier)));
        
        var parameters = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "redirect_uri", Uri.EscapeDataString(authRedirectUri) },
            { "client_id", Uri.EscapeDataString(_clientId) },
            { "lang", "EN" },
            { "code_challenge", Uri.EscapeDataString(this.CodeChallenge) },
            { "code_challenge_method", "S256" },
            { "scope", Uri.EscapeDataString("collection.read collection.write playlists.read playlists.write user.read") }
        };

        var queryString = new StringBuilder();
        foreach (var param in parameters)
        {
            queryString.Append($"&{param.Key}={param.Value}");
        }

        return $"https://login.tidal.com/authorize?{queryString}";
    }
    
    public async Task<TidalAuthenticationResponse?> AuthenticateWithCodeAsync(string redirectUri, string authorizationCode)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal AuthenticateWithCode");

        var token = await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(AuthTokenUrl);
            
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("client_id", _clientId);
            request.AddParameter("code", authorizationCode);
            request.AddParameter("redirect_uri", redirectUri);
            request.AddParameter("code_verifier", this.CodeVerifier);
            
            return await client.PostAsync<TidalAuthenticationResponse>(request);
        });

        if (token != null)
        {
            this.AuthenticationResponse = token;
        }
        return token;
    }
    
    public async Task<TidalAuthenticationResponse?> AuthenticateWithRefreshTokenAsync(string refreshToken)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal Authenticate With RefreshToken");

        var token = await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(AuthTokenUrl);
            
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", refreshToken);
            request.AddParameter("client_id", _clientId);
            
            return await client.PostAsync<TidalAuthenticationResponse>(request);
        });

        if (token != null)
        {
            this.AuthenticationResponse = token;
        }
        return token;
    }
    public async Task<CreatePlaylistResponse?> CreatePlaylistAsync(string name)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal CreatePlaylist");

        CreatePlaylistRequest playlistRequest = new CreatePlaylistRequest
        {
            Data = new CreatePlaylistRequestData
            {
                Type = "playlists",
                Attributes = new CreatePlaylistDataRequestAttributes
                {
                    AccessType = "PUBLIC",
                    Description = string.Empty,
                    Name = name,
                }
            }
        };

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(PlaylistsUrl);
            
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddJsonBody(playlistRequest);
            
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddHeader("countryCode", _countryCode);
            
            return await client.PostAsync<CreatePlaylistResponse>(request);
        });
    }
    
    
    public async Task<TidalSearchResponse?> SearchResultsTracksAsync(string searchTerm)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal SearchResults Tracks '{searchTerm}'");
        using RestClient client = new RestClient(SearchResultArtistsUrl + Uri.EscapeDataString(searchTerm));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("countryCode", _countryCode);
            request.AddParameter("include", "tracks,artists,albums");
            
            return await client.GetAsync<TidalSearchResponse>(request);
        });
    }
    public async Task<TidalSearchTracksNextResponse?> GetTracksNextFromSearchAsync(string next)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetTracksNextFromSearch");
        
        string url = $"{TidalApiPrefix}{next}";

        if (!url.Contains("include="))
        {
            url += "&include=tracks,artists,albums";
        }
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            
            return await client.GetAsync<TidalSearchTracksNextResponse>(request);
        });
    }
    public async Task<TidalTrackArtistResponse?> GetTrackArtistsByTrackIdAsync(int[] trackIds)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetTrackArtistsByTrackId for {trackIds.Length} tracks");
        using RestClient client = new RestClient(TracksUrl);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("filter[id]", string.Join(',', trackIds));
            request.AddParameter("include", "artists");
            request.AddParameter("countryCode", _countryCode);
            
            return await client.GetAsync<TidalTrackArtistResponse>(request);
        });
    }
    public async Task<TidalSearchArtistNextResponse?> GetAlbumSelfInfoAsync(string selfLink)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetAlbumSelfInfo");

        string url = $"{TidalApiPrefix}{selfLink}";
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            return await client.GetAsync<TidalSearchArtistNextResponse>(request);
        });
    }
    public async Task<TidalSearchResponse?> GetTracksByAlbumIdAsync(int albumId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetTracksByAlbumId '{albumId}'");
        using RestClient client = new RestClient(string.Format(TracksByAlbumIdUrl, albumId));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("countryCode", _countryCode);
            request.AddParameter("include", "artists,coverArt,items,providers");
            
            return await client.GetAsync<TidalSearchResponse>(request);
        });
    }
    public async Task<TidalSearchTracksNextResponse?> GetTracksNextByAlbumIdAsync(int albumId, string next)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetTracksNextByAlbumId '{albumId}'");
        
        string url = $"{TidalApiPrefix}{next}";

        if (!url.Contains("include="))
        {
            url += "&include=artists,coverArt,items,providers";
        }
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");

            return await client.GetAsync<TidalSearchTracksNextResponse>(request);
        });
    }
    public async Task AddTrackToPlaylistAsync(string playlistId, string trackId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal AddTrackToPlaylist '{playlistId}'");
        
        string url = string.Format(PlaylistPostItemsUrl, playlistId);
        using RestClient client = new RestClient(url);

        PlaylistPostItemsRequest playlistRequest = new PlaylistPostItemsRequest
        {
            Data = [new PlaylistPostItemsDataRequest
            {
                Id = trackId,
                Type = "tracks"
            }]
        };

        await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            request.AddJsonBody(playlistRequest);
            request.AddHeader("Authorization", $"Bearer {this.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            await client.ExecutePostAsync(request);
        });
    }

    private static string ToBase64UrlEncoded(byte[] data) => 
        Convert.ToBase64String(data).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    
    private AsyncRetryPolicy GetRetryPolicy()
    {
        AsyncRetryPolicy retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(5, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) => {
                    Debug.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                });
        
        return retryPolicy;
    }
}