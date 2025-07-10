namespace MiniMediaPlaylists.Models.Plex;

public class PlexMediaContainer<T>
{
    public int Size { get; set; }
    public List<T> Metadata { get; set; }
    public List<T> SearchResult { get; set; }
}