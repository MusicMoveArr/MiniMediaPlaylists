namespace MiniMediaPlaylists.Models;

public class GenericPlaylist
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool CanAddTracks { get; set; }
    public bool CanSortTracks { get; set; }
}