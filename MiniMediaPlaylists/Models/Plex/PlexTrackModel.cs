namespace MiniMediaPlaylists.Models.Plex;

public class PlexTrackModel
{
    public string RatingKey { get; set; }
    public string Key { get; set; }
    public string Type { get; set; }
    public string Title { get; set; }
    public string Guid { get; set; }
    public string ParentStudio { get; set; }
    public string LibrarySectionTitle { get; set; }
    public int LibrarySectionId { get; set; }
    public string GrandparentTitle { get; set; }
    public float UserRating { get; set; }
    public string ParentTitle { get; set; }
    public int ParentYear { get; set; }
    public int PlaylistItemId { get; set; }
    public long Duration { get; set; }
    public long LastViewedAt { get; set; }
    public long LastRatedAt { get; set; }
    public long AddedAt { get; set; }
    public string MusicAnalysisVersion { get; set; }
    public List<PlexTrackMediaModel> Media { get; set; }
}