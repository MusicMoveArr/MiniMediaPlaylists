using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaPlaylists.Models;

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

    [CommandOption("keep-hourly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSUBSONIC_KEEP_HOURLY")]
    public int RetentionKeepHourly { get; init; } = 24;

    [CommandOption("keep-daily",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSUBSONIC_KEEP_DAILY")]
    public int RetentionKeepDaily { get; init; } = 7;

    [CommandOption("keep-weekly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSUBSONIC_KEEP_WEEKLY")]
    public int RetentionKeepWeekly { get; init; } = 4;

    [CommandOption("keep-monthly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSUBSONIC_KEEP_MOTHLY")]
    public int RetentionKeepMonthly { get; init; } = 12;

    [CommandOption("keep-yearly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSUBSONIC_KEEP_YEARLY")]
    public int RetentionKeepYearly { get; init; } = 10;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new PullSubSonicCommandHandler(ConnectionString);
        var retentionPolicy = new RetentionPolicy
        {
            KeepHourly = RetentionKeepHourly,
            KeepDaily = RetentionKeepDaily,
            KeepWeekly = RetentionKeepWeekly,
            KeepMonthly = RetentionKeepMonthly,
            KeepYearly = RetentionKeepYearly
        };

        await handler.PullSubSonicPlaylists(ServerUrl, Username, Password, LikedSongsPlaylistName, retentionPolicy);
    }
}