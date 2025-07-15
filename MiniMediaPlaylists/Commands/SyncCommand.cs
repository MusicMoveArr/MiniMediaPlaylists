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
    
    [CommandOption("from-skip-playlists",
        Description = "Skip to sync by playlist names.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_FROM_SKIP_PLAYLISTS")]
    public List<string> FromSkipPlaylists { get; init; } = new List<string>();
    
    [CommandOption("from-skip-prefix-playlists",
        Description = "Skip to sync by playlists that start with prefix(es).",
        IsRequired = false,
        EnvironmentVariable = "SYNC_FROM_SKIP_PREFIX_PLAYLISTS")]
    public List<string> FromSkipPrefixPlaylists { get; init; } = new List<string>();
    
    [CommandOption("from-tidal-country-code", 
        Description = "Tidal's CountryCode (e.g. US, FR, NL, DE etc).",
        IsRequired = false,
        EnvironmentVariable = "SYNC_FROM_TIDAL_COUNTRY_CODE")]
    public string FromTidalCountryCode { get; init; }
    
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
    
    [CommandOption("to-tidal-country-code", 
        Description = "Tidal's CountryCode (e.g. US, FR, NL, DE etc).",
        IsRequired = false,
        EnvironmentVariable = "SYNC_TO_TIDAL_COUNTRY_CODE")]
    public string ToTidalCountryCode { get; init; }

    [CommandOption("match-percentage",
        Description = "The required amount of % to match a playlist track.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_MATCHPERCENTAGE")]
    public int MatchPercentage { get; init; } = 90;

    [CommandOption("like-playlist-name",
        Description = "The name of the like/favorite songs playlist, when using this setting it will like/favorite tracks instead of adding them to a target playlist.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_LIKE_PLAYLIST_NAME")]
    public string LikePlaylistName { get; init; }

    [CommandOption("force-add-track",
        Description = "Ignore thinking a song was already added to the playlist and try again anyway, useful for recovering backups.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_FORCE_ADD_TRACK")]
    public bool ForceAddTrack { get; init; }

    [CommandOption("deep-search",
        Description = "If the desired track cannot be found with the normal search method, we'll do a automated search ourself going through the artists, albums to find it.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_DEEP_SEARCH")]
    public bool DeepSearchThroughArtist { get; init; }


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
            FromSkipPlaylists = FromSkipPlaylists,
            FromSkipPrefixPlaylists = FromSkipPrefixPlaylists,
            FromTidalCountryCode = FromTidalCountryCode,
            
            ToService = ToService,
            ToName = ToName,
            ToPlaylistName = ToPlaylistName,
            ToPlaylistPrefix = ToPlaylistPrefix,
            ToPlexToken = ToPlexToken,
            ToSubSonicUsername = ToSubSonicUsername,
            ToSubSonicPassword = ToSubSonicPassword,
            ToTidalCountryCode = ToTidalCountryCode,
            
            MatchPercentage = MatchPercentage,
            LikePlaylistName = LikePlaylistName,
            ForceAddTrack = ForceAddTrack,
            DeepSearchThroughArtist = DeepSearchThroughArtist,
        };

        await handler.SyncPlaylists(syncConfig);
    }
}