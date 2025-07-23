namespace MiniMediaPlaylists.Models.Jellyfin;

public class ItemsResponse<T>
{
    public List<T> Items { get; set; }
    public int TotalRecordCount { get; set; }
    public int StartIndex { get; set; }
}