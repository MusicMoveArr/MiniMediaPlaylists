namespace MiniMediaPlaylists.Models.Tidal;

public class TrackByIdIncludedAttributes
{
    public string Title { get; set; }
    public string BarcodeId { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string ReleaseDate { get; set; }
    public bool Explicit { get; set; }
}