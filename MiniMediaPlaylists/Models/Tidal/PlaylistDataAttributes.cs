namespace MiniMediaPlaylists.Models.Tidal;

public class PlaylistDataAttributes
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Bounded { get; set; }
    public string Duration { get; set; }
    public int NumberOfItems { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public string Privacy { get; set; }
    public string AccessType { get; set; }
    public string PlaylistType { get; set; }
}