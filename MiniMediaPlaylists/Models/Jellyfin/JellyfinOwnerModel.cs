namespace MiniMediaPlaylists.Models.Jellyfin;

public class JellyfinOwnerModel
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string JellyfinUserId { get; set; }
    public string AccessToken { get; set; }
    public string ServerUrl { get; set; }
    public DateTime LastSyncTime { get; set; }
}