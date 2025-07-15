namespace MiniMediaPlaylists.Models.Tidal;

public class TrackByIdIncluded
{
    public string Id { get; set; }
    public string Type { get; set; }
    public TrackByIdIncludedAttributes Attributes { get; set; }
}