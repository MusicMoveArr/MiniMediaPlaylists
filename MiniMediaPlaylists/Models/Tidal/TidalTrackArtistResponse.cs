namespace MiniMediaPlaylists.Models.Tidal;

public class TidalTrackArtistResponse
{
    public List<TidalTrackArtistEntityResponse> Data { get; set; }
    public List<TidalTrackArtistIncludedEntity> Included { get; set; }
}