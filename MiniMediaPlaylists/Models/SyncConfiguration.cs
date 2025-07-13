namespace MiniMediaPlaylists.Models;

public class SyncConfiguration
{
    public const string ServicePlex = "plex";
    public const string ServiceSpotify = "spotify";
    public const string ServiceSubsonic = "subsonic";
    
    public required string FromService { get; init; }
    public required string FromName { get; init; }
    public required string FromPlaylistName { get; init; }
    public required string FromPlexToken { get; init; }
    public required string FromSubSonicUsername { get; init; }
    public required string FromSubSonicPassword { get; init; }
    public required List<string> FromSkipPlaylists { get; init; }
    public required List<string> FromSkipPrefixPlaylists { get; init; }
    
    public required string ToService { get; init; }
    public required string ToName { get; init; }
    public required string ToPlaylistName { get; init; }
    public required string ToPlaylistPrefix { get; init; }
    public required string ToPlexToken { get; init; }
    public required string ToSubSonicUsername { get; init; }
    public required string ToSubSonicPassword { get; init; }
    
    public required int MatchPercentage { get; init; }
    public required string LikePlaylistName { get; init; }
    
    public required bool ForceAddTrack { get; init; }
    public required bool DeepSearchThroughArtist { get; init; }
}