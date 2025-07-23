namespace MiniMediaPlaylists.Models.Jellyfin;

public class JellyfinTrackItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ChannelId { get; set; }
    public string ServerId { get; set; }
    public string PlayListItemId { get; set; }
    public string Container { get; set; }
    public DateTime? PremiereDate { get; set; }
    public int ProductionYear { get; set; }
    public int IndexNumber { get; set; }
    public bool IsFolder { get; set; }
    public List<string> Artists { get; set; }
    public string Album { get; set; }
    public string AlbumArtist { get; set; }
    public JellyfinTrackItemUserdata UserData { get; set; }
    public string LocationType { get; set; }
    public string MediaType { get; set; }
}