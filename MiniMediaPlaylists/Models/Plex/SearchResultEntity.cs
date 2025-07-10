namespace MiniMediaPlaylists.Models.Plex;

public class SearchResultEntity<T>
{
    public float Score { get; set; }
    public T Metadata { get; set; }
}