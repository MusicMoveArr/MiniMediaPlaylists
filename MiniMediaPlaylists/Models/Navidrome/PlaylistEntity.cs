namespace MiniMediaPlaylists.Models.Navidrome;

public class PlaylistEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Comment { get; set; }
    public double Duration { get; set; }
    public long Size { get; set; }
    public int SongCount { get; set; }
    public string OwnerName { get; set; }
    public Guid OwnerId { get; set; }
    public string Public { get; set; }
    public string Path { get; set; }
    public bool Sync { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}