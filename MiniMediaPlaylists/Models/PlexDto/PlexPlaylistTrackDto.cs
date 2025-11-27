namespace MiniMediaPlaylists.Models.PlexDto;

public class PlexPlaylistTrackDto
{
    public string RatingKey { get; set; }
    public string PlaylistId { get; set; }
    public Guid ServerId { get; set; }
    public string Key { get; set; }
    public string Type { get; set; }
    public string Title { get; set; }
    public string Guid { get; set; }
    public string ParentStudio { get; set; }
    public string LibrarySectionTitle { get; set; }
    public int LibrarySectionId { get; set; }
    public string GrandParentTitle { get; set; }
    public float UserRating { get; set; }
    public string ParentTitle { get; set; }
    public int ParentYear { get; set; }
    public int MusicAnalysisVersion { get; set; }
    public long MediaId { get; set; }
    public long MediaPartId { get; set; }
    public string MediaPartKey { get; set; }
    public long MediaPartDuration { get; set; }
    public string MediaPartFile { get; set; }
    public string MediaPartContainer { get; set; }
    public bool IsRemoved { get; set; }
    public DateTime LastViewedAt { get; set; }
    public DateTime LastRatedAt { get; set; }
    public DateTime AddedAt { get; set; }
    public Guid SnapshotId { get; set; }
    public int Playlist_SortOrder { get; set; }
    public int Playlist_ItemId { get; set; }
    
    public static readonly List<string> PlaylistTrackDtoColumnNames =
    [
        nameof(RatingKey),
        nameof(PlaylistId),
        nameof(ServerId),
        nameof(Key),
        nameof(Type),
        nameof(Title),
        nameof(Guid),
        nameof(ParentStudio),
        nameof(LibrarySectionTitle),
        nameof(LibrarySectionId),
        nameof(GrandParentTitle),
        nameof(UserRating),
        nameof(ParentTitle),
        nameof(ParentYear),
        nameof(MusicAnalysisVersion),
        nameof(MediaId),
        nameof(MediaPartId),
        nameof(MediaPartKey),
        nameof(MediaPartDuration),
        nameof(MediaPartFile),
        nameof(MediaPartContainer),
        nameof(IsRemoved),
        nameof(LastViewedAt),
        nameof(LastRatedAt),
        nameof(AddedAt),
        nameof(SnapshotId),
        nameof(Playlist_SortOrder),
        nameof(Playlist_ItemId)
    ];
}