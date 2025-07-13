using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaPlaylists.Commands;

[Command("pullsubsonic", Description = "Pull all your SubSonic playlists")]
public class PullSubSonicCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("url", 
        Description = "SubSonic server url.", 
        EnvironmentVariable = "PULLSUBSONIC_URL",
        IsRequired = true)]
    public required string ServerUrl { get; init; }
    
    [CommandOption("username", 
        Description = "SubSonic username for authentication.", 
        IsRequired = true,
        EnvironmentVariable = "PULLSUBSONIC_USERNAME")]
    public required string Username { get; init; }
    
    [CommandOption("password", 
        Description = "SubSonic password for authentication.", 
        IsRequired = true,
        EnvironmentVariable = "PULLSUBSONIC_PASSWORD")]
    public required string Password { get; init; }
    
    [CommandOption("liked-playlist-name", 
        Description = "Save the liked songs into a specific playlist name, in SubSonic liked songs are not in a playlist.", 
        IsRequired = false,
        EnvironmentVariable = "PULLSUBSONIC_LIKED_PLAYLIST_NAME")]
    public string LikedSongsPlaylistName { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new PullSubSonicCommandHandler(ConnectionString);

        await handler.PullSubSonicPlaylists(ServerUrl, Username, Password, LikedSongsPlaylistName);
    }
}