namespace MiniMediaPlaylists.Models.Jellyfin;

public class AuthenticationResponse
{
    public AuthenticationUserResponse User { get; set; }
    public string AccessToken { get; set; }
    public string ServerId { get; set; }
}