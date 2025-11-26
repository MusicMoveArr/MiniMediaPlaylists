namespace MiniMediaPlaylists.Models.SpotifyDto;

public class SpotifyPlaylistDto
{
    public string Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Href { get; set; }
    public string Name { get; set; }
    public int TrackCount { get; set; }
    public string Uri { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid SnapshotId { get; set; }
    
    
    public static readonly List<string> PlaylistDtoColumnNames =
    [
        nameof(Id),
        nameof(OwnerId),
        nameof(Href),
        nameof(Name),
        nameof(TrackCount),
        nameof(Uri),
        nameof(AddedAt),
        nameof(UpdatedAt),
        nameof(SnapshotId)
    ];
}