namespace MiniMediaPlaylists.Models.Jellyfin;

public class CreatePlaylistRequest
{
    public List<string> Ids { get; set; }
    public bool IsPublic { get; set; }
    public string Name { get; set; }
    public string UserId { get; set; }
}