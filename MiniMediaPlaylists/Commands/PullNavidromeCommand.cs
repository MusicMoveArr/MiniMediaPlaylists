using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaPlaylists.Models;

namespace MiniMediaPlaylists.Commands;

[Command("pullnavidrome", Description = "Pull all your Navidrome playlists")]
public class PullNavidromeCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("url", 
        Description = "Navidrome server url.", 
        EnvironmentVariable = "PULLNAVIDROME_URL",
        IsRequired = true)]
    public required string ServerUrl { get; init; }
    
    [CommandOption("username", 
        Description = "Navidrome username for authentication.", 
        IsRequired = true,
        EnvironmentVariable = "PULLNAVIDROME_USERNAME")]
    public required string Username { get; init; }
    
    [CommandOption("password", 
        Description = "Navidrome password for authentication.", 
        IsRequired = true,
        EnvironmentVariable = "PULLNAVIDROME_PASSWORD")]
    public required string Password { get; init; }
    
    [CommandOption("liked-playlist-name", 
        Description = "Save the liked songs into a specific playlist name, in Navidrome liked songs are not in a playlist.", 
        IsRequired = false,
        EnvironmentVariable = "PULLNAVIDROME_LIKED_PLAYLIST_NAME")]
    public string LikedSongsPlaylistName { get; init; }

    [CommandOption("keep-hourly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLNAVIDROME_KEEP_HOURLY")]
    public int RetentionKeepHourly { get; init; } = 24;

    [CommandOption("keep-daily",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLNAVIDROME_KEEP_DAILY")]
    public int RetentionKeepDaily { get; init; } = 7;

    [CommandOption("keep-weekly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLNAVIDROME_KEEP_WEEKLY")]
    public int RetentionKeepWeekly { get; init; } = 4;

    [CommandOption("keep-monthly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLNAVIDROME_KEEP_MOTHLY")]
    public int RetentionKeepMonthly { get; init; } = 12;

    [CommandOption("keep-yearly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLNAVIDROME_KEEP_YEARLY")]
    public int RetentionKeepYearly { get; init; } = 10;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new PullNavidromeCommandHandler(ConnectionString);
        var retentionPolicy = new RetentionPolicy
        {
            KeepHourly = RetentionKeepHourly,
            KeepDaily = RetentionKeepDaily,
            KeepWeekly = RetentionKeepWeekly,
            KeepMonthly = RetentionKeepMonthly,
            KeepYearly = RetentionKeepYearly
        };

        await handler.PullNavidromePlaylists(ServerUrl, Username, Password, LikedSongsPlaylistName, retentionPolicy);
    }
}