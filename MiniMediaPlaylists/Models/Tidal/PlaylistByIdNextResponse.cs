namespace MiniMediaPlaylists.Models.Tidal;

public class PlaylistByIdNextResponse
{
    public List<PlaylistDataRelationshipsItemsData> Data { get; set; }
    public List<PlaylistIncluded>  Included { get; set; }
    public PlaylistDataRelationshipsItemsLinks Links { get; set; }
}