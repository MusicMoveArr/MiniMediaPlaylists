namespace MiniMediaPlaylists.Models.SpotifyDto;

public class SpotifyPlaylistTrackDto
{
    public string Id { get; set; }
    public string PlaylistId { get; set; }
    public Guid OwnerId { get; set; }
    public string AlbumType { get; set; }
    public string AlbumId { get; set; }
    public string AlbumName { get; set; }
    public string AlbumReleaseDate { get; set; }
    public string AlbumTotalTracks { get; set; }
    public string ArtistName { get; set; }
    public string Name { get; set; }
    public string AddedById { get; set; }
    public string AddedByType { get; set; }
    public bool IsRemoved { get; set; }
    public DateTime AddedAt { get; set; }
    public Guid SnapshotId { get; set; }
    public int Playlist_SortOrder { get; set; }
    
    public static readonly List<string> PlaylistTrackDtoColumnNames =
    [
        nameof(Id),
        nameof(PlaylistId),
        nameof(OwnerId),
        nameof(AlbumType),
        nameof(AlbumId),
        nameof(AlbumName),
        nameof(AlbumReleaseDate),
        nameof(AlbumTotalTracks),
        nameof(ArtistName),
        nameof(Name),
        nameof(AddedById),
        nameof(AddedByType),
        nameof(IsRemoved),
        nameof(AddedAt),
        nameof(SnapshotId),
        nameof(Playlist_SortOrder)
    ];
}