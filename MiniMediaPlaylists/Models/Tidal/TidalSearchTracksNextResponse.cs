namespace MiniMediaPlaylists.Models.Tidal;

public class TidalSearchTracksNextResponse
{
    public List<TidalAlbumItemsDataEntity>? Data { get; set; }
    public TidalRelationShipsAlbumsLinksEntity? Links { get; set; }
    public List<TidalSearchDataEntity>? Included { get; set; }
}