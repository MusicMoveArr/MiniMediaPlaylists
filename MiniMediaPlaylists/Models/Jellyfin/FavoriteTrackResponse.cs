namespace MiniMediaPlaylists.Models.Jellyfin;

public class FavoriteTrackResponse
{
    public bool IsFavorite { get; set; }
    public int PlaybackPositionTicks { get; set; }
    public int PlayCount { get; set; }
    public bool Played { get; set; }
    public string Key { get; set; }
    public string ItemId { get; set; }
}