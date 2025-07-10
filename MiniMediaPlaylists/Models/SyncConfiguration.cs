namespace MiniMediaPlaylists.Models;

public class SyncConfiguration
{
    public required string FromService { get; init; }
    public required string FromName { get; init; }
    public required string FromPlaylistName { get; init; }
    public required string FromPlexToken { get; init; }
    public required string FromSubSonicUsername { get; init; }
    public required string FromSubSonicPassword { get; init; }
    
    public required string ToService { get; init; }
    public required string ToName { get; init; }
    public required string ToPlaylistName { get; init; }
    public required string ToPlaylistPrefix { get; init; }
    public required string ToPlexToken { get; init; }
    public required string ToSubSonicUsername { get; init; }
    public required string ToSubSonicPassword { get; init; }
    
    public required int MatchPercentage { get; init; }
}