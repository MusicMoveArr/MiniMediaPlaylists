namespace MiniMediaPlaylists.Models.Tidal;

public class PlaylistByIdResponse
{
    public PlaylistData Data { get; set; }
    public List<PlaylistIncluded> Included { get; set; }
}