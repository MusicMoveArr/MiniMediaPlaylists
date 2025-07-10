using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaPlaylists.Models;

namespace MiniMediaPlaylists.Commands;

[Command("sync", Description = "Sync playlists between 2 services")]
public class SyncPlexCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("from-service", 
        Description = "Sync from the selected service.", 
        EnvironmentVariable = "SYNC_FROM_SERVICE",
        IsRequired = true)]
    public required string FromService { get; init; }
    
    [CommandOption("from-name", 
        Description = "Sync from either the name (username etc) or url.", 
        EnvironmentVariable = "SYNC_FROM_NAME",
        IsRequired = true)]
    public required string FromName { get; init; }
    
    [CommandOption("from-playlist-name", 
        Description = "Sync from this specific playlist name.", 
        EnvironmentVariable = "SYNC_FROM_PLAYLISTNAME",
        IsRequired = false)]
    public string FromPlaylistName { get; init; }
    
    [CommandOption("from-plex-token", 
        Description = "Plex token for authentication.", 
        EnvironmentVariable = "SYNC_FROM_PLEX_TOKEN",
        IsRequired = false)]
    public string FromPlexToken { get; init; }
    
    [CommandOption("from-subsonic-username", 
        Description = "SubSonic username for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "SYNC_FROM_SUBSONIC_USERNAME")]
    public string FromSubSonicUsername { get; init; }
    
    [CommandOption("from-subsonic-password", 
        Description = "SubSonic password for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "SYNC_FROM_SUBSONIC_PASSWORD")]
    public string FromSubSonicPassword { get; init; }
    
    [CommandOption("to-service", 
        Description = "Sync to the selected service.", 
        EnvironmentVariable = "SYNC_TO_SERVICE",
        IsRequired = true)]
    public required string ToService { get; init; }
    
    [CommandOption("to-name", 
        Description = "Sync to either the name or url.", 
        EnvironmentVariable = "SYNC_TO_NAME",
        IsRequired = true)]
    public required string ToName { get; init; }
    
    [CommandOption("to-playlist-name", 
        Description = "Sync to this specific playlist name.", 
        EnvironmentVariable = "SYNC_TO_PLAYLISTNAME",
        IsRequired = false)]
    public string ToPlaylistName { get; init; }

    [CommandOption("to-playlist-prefix",
        Description = "Sync to this specific playlist name.",
        EnvironmentVariable = "SYNC_TO_PLAYLISTPREFIX",
        IsRequired = false)]
    public string ToPlaylistPrefix { get; init; } = string.Empty;
    
    [CommandOption("to-plex-token", 
        Description = "Plex token for authentication.", 
        EnvironmentVariable = "SYNC_TO_PLEX_TOKEN",
        IsRequired = false)]
    public string ToPlexToken { get; init; }
    
    
    [CommandOption("to-subsonic-username", 
        Description = "SubSonic username for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "SYNC_TO_SUBSONIC_USERNAME")]
    public string ToSubSonicUsername { get; init; }
    
    [CommandOption("to-subsonic-password", 
        Description = "SubSonic password for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "SYNC_TO_SUBSONIC_PASSWORD")]
    public string ToSubSonicPassword { get; init; }

    [CommandOption("match-percentage",
        Description = "The required amount of % to match a playlist track.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_MATCHPERCENTAGE")]
    public int MatchPercentage { get; init; } = 90;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new SyncCommandHandler(ConnectionString);
        var syncConfig = new SyncConfiguration
        {
            FromService = FromService,
            FromName = FromName,
            FromPlaylistName = FromPlaylistName,
            FromPlexToken = FromPlexToken,
            FromSubSonicUsername = FromSubSonicUsername,
            FromSubSonicPassword = FromSubSonicPassword,
            
            ToService = ToService,
            ToName = ToName,
            ToPlaylistName = ToPlaylistName,
            ToPlaylistPrefix = ToPlaylistPrefix,
            ToPlexToken = ToPlexToken,
            ToSubSonicUsername = ToSubSonicUsername,
            ToSubSonicPassword = ToSubSonicPassword,
            
            MatchPercentage = MatchPercentage
        };

        await handler.SyncPlaylists(syncConfig);
    }
}