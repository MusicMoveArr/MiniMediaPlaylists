namespace MiniMediaPlaylists.Models.Plex;

public class PlaylistModel
{
    public string RatingKey { get; set; }
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
    public long LastViewedAt { get; set; }
    public long Duration { get; set; }
    public long LeafCount { get; set; }
    public long AddedAt { get; set; }
    public long UpdatedAt { get; set; }
}