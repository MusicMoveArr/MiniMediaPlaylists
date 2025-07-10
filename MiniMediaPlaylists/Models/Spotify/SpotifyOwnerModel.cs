namespace MiniMediaPlaylists.Models.Spotify;

public class SpotifyOwnerModel
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; }
    public DateTime LastSyncTime { get; set; }
    public string AuthClientId { get; set; }
    public string AuthSecretId { get; set; }
    public string AuthRefreshToken { get; set; }
}