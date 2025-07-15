namespace MiniMediaPlaylists.Models.Tidal;

public class TidalSearchArtistNextResponse
{
    public List<TidalRelationShipsAlbumsDataEntity> Data { get; set; }
    public TidalRelationShipsAlbumsLinksEntity Links { get; set; }
    public List<TidalSearchDataEntity> Included { get; set; }
}