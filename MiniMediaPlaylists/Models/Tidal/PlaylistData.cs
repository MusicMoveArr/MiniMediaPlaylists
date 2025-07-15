namespace MiniMediaPlaylists.Models.Tidal;

public class PlaylistData
{
    public string Id { get; set; }
    public string Type { get; set; }
    public PlaylistDataAttributes Attributes { get; set; }
    public PlaylistDataRelationships Relationships { get; set; }
}