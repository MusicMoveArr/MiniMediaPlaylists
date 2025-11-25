namespace MiniMediaPlaylists.Models.SubsonicDto;

public class SubsonicPlaylistDto
{
    public string Id { get; set; }
    public Guid ServerId { get; set; }
    public DateTime ChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Comment { get; set; }
    public int Duration { get; set; }
    public string Name { get; set; }
    public string Owner { get; set; }
    public bool Public { get; set; }
    public int SongCount { get; set; }
    public Guid SnapshotId { get; set; }
    
    
    public static readonly List<string> PlaylistDtoColumnNames =
    [
        nameof(Id),
        nameof(ServerId),
        nameof(ChangedAt),
        nameof(CreatedAt),
        nameof(Comment),
        nameof(Duration),
        nameof(Name),
        nameof(Owner),
        nameof(Public),
        nameof(SongCount),
        nameof(SnapshotId)
    ];
}