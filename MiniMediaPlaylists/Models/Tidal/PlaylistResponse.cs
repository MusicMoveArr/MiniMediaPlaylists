namespace MiniMediaPlaylists.Models.Tidal;

public class PlaylistResponse
{
    public List<PlaylistData> Data { get; set; }
    public List<PlaylistIncluded> Included { get; set; }
    public PlaylistDataRelationshipsItemsLinks Links { get; set; }
}