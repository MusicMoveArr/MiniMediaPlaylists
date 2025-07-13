using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaPlaylists.Commands;

[Command("pullplex", Description = "Pull all your Plex playlists")]
public class PullSPlexCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("url", 
        Description = "Plex server url.", 
        EnvironmentVariable = "PULLPLEX_URL",
        IsRequired = true)]
    public required string ServerUrl { get; init; }
    
    [CommandOption("token", 
        Description = "Plex token for authentication.", 
        IsRequired = true,
        EnvironmentVariable = "PULLPLEX_TOKEN")]
    public required string Token { get; init; }

    [CommandOption("track-limit",
        Description = "Set the playlist track limit to pull.",
        IsRequired = false,
        EnvironmentVariable = "PULLPLEX_TRACK_LIMIT")]
    public int TrackLimit { get; init; } = 5000;
    

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new PullPlexCommandHandler(ConnectionString);

        await handler.PullPlexPlaylists(ServerUrl, Token, TrackLimit);
    }
}