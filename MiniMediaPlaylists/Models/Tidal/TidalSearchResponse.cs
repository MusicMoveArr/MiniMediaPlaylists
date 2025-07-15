namespace MiniMediaPlaylists.Models.Tidal;

public class TidalSearchResponse
{
    public TidalSearchDataEntity? Data { get; set; }
    
    public List<TidalSearchDataEntity>? Included { get; set; }
}