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
    
    
    [CommandOption("from-jellyfin-username", 
        Description = "Jellyfin username for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "SYNC_FROM_JELLYFIN_USERNAME")]
    public string FromJellyfinUsername { get; init; }
    
    [CommandOption("from-jellyfin-password", 
        Description = "Jellyfin password for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "SYNC_FROM_JELLYFIN_PASSWORD")]
    public string FromJellyfinPassword { get; init; }
    
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
    
    [CommandOption("to-jellyfin-username", 
        Description = "Jellyfin username for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "SYNC_TO_JELLYFIN_USERNAME")]
    public string ToJellyfinUsername { get; init; }
    
    [CommandOption("to-jellyfin-password", 
        Description = "Jellyfin password for authentication.", 
        IsRequired = false,
        EnvironmentVariable = "SYNC_TO_JELLYFIN_PASSWORD")]
    public string ToJellyfinPassword { get; init; }
    
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

    [CommandOption("from-like-playlist-name",
        Description = "The name of the like/favorite songs playlist, when using this setting it will like/favorite tracks instead of adding them to a target playlist.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_FROM_LIKE_PLAYLIST_NAME")]
    public string FromLikePlaylistName { get; init; }

    [CommandOption("to-like-playlist-name",
        Description = "The name of the like/favorite songs playlist, when using this setting it will like/favorite tracks instead of adding them to a target playlist.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_TO_LIKE_PLAYLIST_NAME")]
    public string ToLikePlaylistName { get; init; }

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

    [CommandOption("playlist-threads",
        Description = "The amount of threads to use for parallel processing.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_PLAYLIST_THREADS")]
    public int PlaylistThreads { get; init; } = 1;
    
    [CommandOption("track-threads",
        Description = "The amount of threads to use for parallel processing each track of a playlist.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_TRACK_THREADS")]
    public int TrackThreads { get; init; } = 1;
    
    [CommandOption("sync-track-order",
        Description = "Synchronize the track order in playlists.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_TRACK_ORDER")]
    public bool SyncTrackOrder { get; init; } = false;
    
    [CommandOption("second-search-without-album",
        Description = "When searching with album cannot find all the tracks, try again without album name.",
        IsRequired = false,
        EnvironmentVariable = "SYNC_SECOND_SEARCH_WITHOUT_ALBUM")]
    public bool SecondSearchWithoutAlbum { get; init; } = false;

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
            FromJellyfinUsername = FromJellyfinUsername,
            FromJellyfinPassword = FromJellyfinPassword,
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
            ToJellyfinUsername = ToJellyfinUsername,
            ToJellyfinPassword = ToJellyfinPassword,
            ToTidalCountryCode = ToTidalCountryCode,
            
            MatchPercentage = MatchPercentage,
            FromLikePlaylistName = FromLikePlaylistName,
            ToLikePlaylistName = ToLikePlaylistName,
            ForceAddTrack = ForceAddTrack,
            DeepSearchThroughArtist = DeepSearchThroughArtist,
            PlaylistThreads = PlaylistThreads,
            TrackThreads = TrackThreads,
            SyncTrackOrder = SyncTrackOrder,
            SecondSearchWithoutAlbum = SecondSearchWithoutAlbum
        };

        await handler.SyncPlaylists(syncConfig);
    }
}