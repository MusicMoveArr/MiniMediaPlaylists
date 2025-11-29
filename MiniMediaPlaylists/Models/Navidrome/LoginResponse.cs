namespace MiniMediaPlaylists.Models.Navidrome;

public class LoginResponse
{
    public string Id { get; set; }
    public bool IsAdmin { get; set; }
    public string Name { get; set; }
    public string SubsonicSalt { get; set; }
    public string SubsonicToken { get; set; }
    public string Token { get; set; }
    public string Username { get; set; }
}