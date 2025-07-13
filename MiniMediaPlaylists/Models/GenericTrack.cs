namespace MiniMediaPlaylists.Models;

public class GenericTrack
{
    public string Id { get; set; }
    public string ArtistName { get; set; }
    public string AlbumName { get; set; }
    public string Title { get; set; }
    public float LikeRating { get; set; }
    public string Uri { get; set; }
}