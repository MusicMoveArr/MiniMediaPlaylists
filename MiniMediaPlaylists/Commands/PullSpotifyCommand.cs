using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaPlaylists.Models;

namespace MiniMediaPlaylists.Commands;

[Command("pullspotify", Description = "Pull all your spotify playlists")]
public class PullSpotifyCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("spotify-client-id",
        Description = "Spotify Client Id, to use for the Spotify API.", 
        IsRequired = true,
        EnvironmentVariable = "PULLSPOTIFY_SPOTIFY_CLIENT_ID")]
    public required string SpotifyClientId { get; init; }
    
    [CommandOption("spotify-secret-id",
        Description = "Spotify Secret Id, to use for the Spotify API.", 
        IsRequired = true,
        EnvironmentVariable = "PULLSPOTIFY_SPOTIFY_SECRET_ID")]
    public required string SpotifySecretId { get; init; }
    
    [CommandOption("authentication-redirect-uri", 
        Description = "The redirect uri to use for Authentication.", 
        IsRequired = true,
        EnvironmentVariable = "PULLSPOTIFY_SPOTIFY_AUTH_REDIRECT_URI")]
    public required string AuthRedirectUri { get; init; }

    [CommandOption("authentication-callback-listener",
        Description = "The callback listener url to use for Authentication.",
        IsRequired = false,
        EnvironmentVariable = "PULLSPOTIFY_SPOTIFY_AUTH_CALLBACK_LISTENER")]
    public string AuthCallbackListener { get; init; } = "http://*:5000/callback/";

    [CommandOption("owner-name",
        Description = "The name of the owner who'se account this belongs to.",
        IsRequired = true,
        EnvironmentVariable = "PULLSPOTIFY_OWNER_NAME")]
    public required string OwnerName { get; init; }
    
    [CommandOption("liked-playlist-name", 
        Description = "Save the liked songs into a specific playlist name, in Spotify liked songs are in 'Liked Songs' which is a fake playlist.", 
        IsRequired = false,
        EnvironmentVariable = "PULLSPOTIFY_LIKED_PLAYLIST_NAME")]
    public string LikedSongsPlaylistName { get; init; }

    
    [CommandOption("keep-hourly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSPOTIFY_KEEP_HOURLY")]
    public int RetentionKeepHourly { get; init; } = 24;

    [CommandOption("keep-daily",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSPOTIFY_KEEP_DAILY")]
    public int RetentionKeepDaily { get; init; } = 7;

    [CommandOption("keep-weekly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSPOTIFY_KEEP_WEEKLY")]
    public int RetentionKeepWeekly { get; init; } = 4;

    [CommandOption("keep-monthly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSPOTIFY_KEEP_MOTHLY")]
    public int RetentionKeepMonthly { get; init; } = 12;

    [CommandOption("keep-yearly",
        Description = "Set retention policy for how many snapshots to keep of playlists.",
        IsRequired = false,
        EnvironmentVariable = "PULLSPOTIFY_KEEP_YEARLY")]
    public int RetentionKeepYearly { get; init; } = 10;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new PullSpotifyCommandHandler(ConnectionString);
        var retentionPolicy = new RetentionPolicy
        {
            KeepHourly = RetentionKeepHourly,
            KeepDaily = RetentionKeepDaily,
            KeepWeekly = RetentionKeepWeekly,
            KeepMonthly = RetentionKeepMonthly,
            KeepYearly = RetentionKeepYearly
        };

        await handler.PullSpotifyPlaylists(
            SpotifyClientId, 
            SpotifySecretId, 
            AuthRedirectUri, 
            AuthCallbackListener, 
            OwnerName, 
            LikedSongsPlaylistName, 
            retentionPolicy);
    }
}