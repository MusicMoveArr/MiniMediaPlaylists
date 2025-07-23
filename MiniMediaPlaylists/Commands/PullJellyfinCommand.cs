using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaPlaylists.Commands;

[Command("pulljellyfin", Description = "Pull all your Jellyfin playlists")]
public class PullJellyfinCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("url", 
        Description = "Jellyfin server url.", 
        EnvironmentVariable = "PULLJELLYFIN_URL",
        IsRequired = true)]
    public required string ServerUrl { get; init; }
    
    [CommandOption("username", 
        Description = "Jellyfin username for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "PULLJELLYFIN_USERNAME")]
    public string Username { get; init; }
    
    [CommandOption("password", 
        Description = "Jellyfin password for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "PULLJELLYFIN_PASSWORD")]
    public string Password { get; init; }
    
    [CommandOption("favorite-playlist-name", 
        Description = "Save the favorite songs into a specific playlist name, in Jellyfin liked songs are not in a playlist.", 
        IsRequired = false,
        EnvironmentVariable = "PULLJELLYFIN_FAVORITE_PLAYLIST_NAME")]
    public string FavoriteSongsPlaylistName { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new PullJellyfinCommandHandler(ConnectionString);
        await handler.PullJellyfinPlaylists(ServerUrl, Username, Password, FavoriteSongsPlaylistName);
    }
}