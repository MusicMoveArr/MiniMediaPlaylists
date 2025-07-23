namespace MiniMediaPlaylists.Models.Jellyfin;

public class JellyfinPlaylistItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ChannelId { get; set; }
    public string ServerId { get; set; }
    public bool IsFolder { get; set; }
    public JellyfinPlaylistItemUserdata UserData { get; set; }
    public string LocationType { get; set; }
    public string MediaType { get; set; }
}