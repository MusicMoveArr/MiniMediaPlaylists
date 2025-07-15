namespace MiniMediaPlaylists.Models.Tidal;

public class CreatePlaylistDataRequest
{
    public string Type { get; set; }
    public CreatePlaylistDataRequestAttributes Attributes { get; set; }
}