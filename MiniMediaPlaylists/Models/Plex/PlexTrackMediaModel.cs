namespace MiniMediaPlaylists.Models.Plex;

public class PlexTrackMediaModel
{
    public long Id { get; set; }
    public int Duration { get; set; }
    public string Container { get; set; }
    public List<PlexTrackMediaPartModel> Part { get; set; }
}