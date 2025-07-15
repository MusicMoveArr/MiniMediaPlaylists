namespace MiniMediaPlaylists.Models.Tidal;

public class TidalRelationShipsEntity
{
    public TidalAlbumItemsEntity Items { get; set; }
    public TidalRelationShipsAlbumsEntity Albums { get; set; }
    public TidalRelationShipsArtistsEntity Artists { get; set; }
    public TidalRelationShipsTracksEntity Tracks { get; set; }
}