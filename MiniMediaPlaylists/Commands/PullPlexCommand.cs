using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaPlaylists.Models;

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

    [CommandOption("keep-hourly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLPLEX_KEEP_HOURLY")]
    public int RetentionKeepHourly { get; init; } = 24;

    [CommandOption("keep-daily",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLPLEX_KEEP_DAILY")]
    public int RetentionKeepDaily { get; init; } = 7;

    [CommandOption("keep-weekly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLPLEX_KEEP_WEEKLY")]
    public int RetentionKeepWeekly { get; init; } = 4;

    [CommandOption("keep-monthly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLPLEX_KEEP_MOTHLY")]
    public int RetentionKeepMonthly { get; init; } = 12;

    [CommandOption("keep-yearly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLPLEX_KEEP_YEARLY")]
    public int RetentionKeepYearly { get; init; } = 10;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new PullPlexCommandHandler(ConnectionString);
        var retentionPolicy = new RetentionPolicy
        {
            KeepHourly = RetentionKeepHourly,
            KeepDaily = RetentionKeepDaily,
            KeepWeekly = RetentionKeepWeekly,
            KeepMonthly = RetentionKeepMonthly,
            KeepYearly = RetentionKeepYearly
        };

        await handler.PullPlexPlaylists(ServerUrl, Token, TrackLimit, retentionPolicy);
    }
}