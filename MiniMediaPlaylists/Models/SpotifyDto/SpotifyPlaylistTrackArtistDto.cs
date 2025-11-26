namespace MiniMediaPlaylists.Models.SpotifyDto;

public class SpotifyPlaylistTrackArtistDto
{
    public string TrackId { get; set; }
    public string ArtistId { get; set; }
    public string AlbumId { get; set; }
    public string ArtistName { get; set; }
    public int Index { get; set; }
    
    public static readonly List<string> PlaylistTrackArtistDtoColumnNames =
    [
        nameof(TrackId),
        nameof(ArtistId),
        nameof(AlbumId),
        nameof(ArtistName),
        nameof(Index)
    ];
}