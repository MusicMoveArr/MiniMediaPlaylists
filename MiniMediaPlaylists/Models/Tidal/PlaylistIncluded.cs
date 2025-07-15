namespace MiniMediaPlaylists.Models.Tidal;

public class PlaylistIncluded
{
    public string Id { get; set; }
    public string Type { get; set; }
    public PlaylistIncludedAttributes Attributes { get; set; }
}