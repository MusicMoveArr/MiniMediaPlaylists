namespace MiniMediaPlaylists.Models.PlexDto;

public class PlexPlaylistDto
{
    public string RatingKey { get; set; }
    public Guid ServerId { get; set; }
    public string Key { get; set; }
    public string Guid { get; set; }
    public string Type { get; set; }
    public string Title { get; set; }
    public string TitleSort { get; set; }
    public string Summary { get; set; }
    public bool Smart { get; set; }
    public string PlaylistType { get; set; }
    public string Composite { get; set; }
    public string Icon { get; set; }
    public DateTime LastViewedAt { get; set; }
    public long Duration { get; set; }
    public long LeafCount { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid SnapshotId { get; set; }
    
    
    public static readonly List<string> PlaylistDtoColumnNames =
    [
        nameof(RatingKey),
        nameof(ServerId),
        nameof(Key),
        nameof(Guid),
        nameof(Type),
        nameof(Title),
        nameof(TitleSort),
        nameof(Summary),
        nameof(Smart),
        nameof(PlaylistType),
        nameof(Composite),
        nameof(Icon),
        nameof(LastViewedAt),
        nameof(Duration),
        nameof(LeafCount),
        nameof(AddedAt),
        nameof(UpdatedAt),
        nameof(SnapshotId)
    ];
}