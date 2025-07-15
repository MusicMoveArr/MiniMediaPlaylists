namespace MiniMediaPlaylists.Models.Tidal;

public class TidalSearchDataEntity
{
    public string Id { get; set; }
    public string Type { get; set; }
    public TidalAttributeEntity Attributes { get; set; }
    public TidalRelationShipsEntity RelationShips { get; set; }
}