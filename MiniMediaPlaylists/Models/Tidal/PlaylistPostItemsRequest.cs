namespace MiniMediaPlaylists.Models.Tidal;

public class PlaylistPostItemsRequest
{
    public List<PlaylistPostItemsDataRequest> Data { get; set; }
    public PlaylistPostItemsMetaRequest Meta { get; set; }
}