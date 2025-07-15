using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaPlaylists.Commands;

[Command("pulltidal", Description = "Pull all your Tidal playlists")]
public class PullTidalCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("client-id", 
        Description = "Tidal username for authentication.", 
        IsRequired = true,
        EnvironmentVariable = "PULLTIDAL_CLIENT_ID")]
    public required string TidalClientId { get; init; }
    
    [CommandOption("secret-id", 
        Description = "Tidal username for authentication.", 
        IsRequired = true,
        EnvironmentVariable = "PULLTIDAL_SECRET_ID")]
    public required string TidalSecretId { get; init; }
    
    [CommandOption("authentication-redirect-uri", 
        Description = "The redirect uri to use for Authentication.", 
        IsRequired = false,
        EnvironmentVariable = "PULLTIDAL_AUTH_REDIRECT_URI")]
    public string AuthRedirectUri { get; init; }
    
    [CommandOption("country-code", 
        Description = "Tidal's CountryCode (e.g. US, FR, NL, DE etc).",
        IsRequired = true,
        EnvironmentVariable = "PULLTIDAL_COUNTRY_CODE")]
    public required string TidalCountryCode { get; init; }

    [CommandOption("owner-name",
        Description = "The name of the owner who'se account this belongs to.",
        IsRequired = true,
        EnvironmentVariable = "PULLTIDAL_OWNER_NAME")]
    public required string OwnerName { get; init; }

    [CommandOption("authentication-callback-listener",
        Description = "The callback listener url to use for Authentication.",
        IsRequired = false,
        EnvironmentVariable = "PULLTIDAL_AUTH_CALLBACK_LISTENER")]
    public string AuthCallbackListener { get; init; } = "http://*:5000/callback/";
    
    [CommandOption("liked-playlist-name", 
        Description = "Save the liked songs into a specific playlist name, in Tidal liked songs are not in a playlist.", 
        IsRequired = false,
        EnvironmentVariable = "PULLTIDAL_LIKED_PLAYLIST_NAME")]
    public string LikedSongsPlaylistName { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new PullTidalCommandHandler(ConnectionString);

        await handler.PullTidalPlaylists(
            TidalClientId, 
            TidalSecretId, 
            TidalCountryCode,
            AuthRedirectUri, 
            AuthCallbackListener, 
            LikedSongsPlaylistName,
            OwnerName);
    }
}