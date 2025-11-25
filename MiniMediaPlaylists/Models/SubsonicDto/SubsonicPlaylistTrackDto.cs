namespace MiniMediaPlaylists.Models.SubsonicDto;

public class SubsonicPlaylistTrackDto
{
    public string Id { get; set; }
    public string PlaylistId { get; set; }
    public Guid ServerId { get; set; }
    public string Album { get; set; }
    public string AlbumId { get; set; }
    public string Artist { get; set; }
    public string ArtistId { get; set; }
    public int Duration { get; set; }
    public string Title { get; set; }
    public string Path { get; set; }
    public long Size { get; set; }
    public int Year { get; set; }
    public bool IsRemoved { get; set; }
    public DateTime AddedAt { get; set; }
    public int UserRating { get; set; }
    public Guid SnapshotId { get; set; }
    public int Playlist_SortOrder { get; set; }
    
    
    
    public static readonly List<string> PlaylistTrackDtoColumnNames =
    [
        nameof(Id),
        nameof(PlaylistId),
        nameof(ServerId),
        nameof(Album),
        nameof(AlbumId),
        nameof(Artist),
        nameof(ArtistId),
        nameof(Duration),
        nameof(Title),
        nameof(Path),
        nameof(Size),
        nameof(Year),
        nameof(IsRemoved),
        nameof(AddedAt),
        nameof(UserRating),
        nameof(SnapshotId),
        nameof(Playlist_SortOrder)
    ];
}