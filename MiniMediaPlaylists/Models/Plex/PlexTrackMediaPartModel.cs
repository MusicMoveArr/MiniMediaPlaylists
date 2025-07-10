namespace MiniMediaPlaylists.Models.Plex;

public class PlexTrackMediaPartModel
{
    public long Id { get; set; }
    public string Key { get; set; }
    public long Duration { get; set; }
    public string File { get; set; }
    public string Container { get; set; }
}