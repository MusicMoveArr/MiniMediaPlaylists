namespace MiniMediaPlaylists.Models.Navidrome;

public class TrackEntity
{
    public string Id { get; set; }
    public string MediaFileId { get; set; }
    public string PlaylistId { get; set; }
    public int Rating { get; set; }
    public DateTime RatedAt { get; set; }
    public bool Starred { get; set; }
    public DateTime StarredAt { get; set; }
    public int BookmarkPosition { get; set; }
    public int LibraryId { get; set; }
    public string LibraryPath { get; set; }
    public string LibraryName { get; set; }
    public string FolderId { get; set; }
    public string Path { get; set; }
    public string Title { get; set; }
    public string Album { get; set; }
    public string ArtistId { get; set; }
    public string Artist { get; set; }
    public string AlbumArtistId { get; set; }
    public string AlbumArtist { get; set; }
    public string AlbumId { get; set; }
    public bool HasCoverArt { get; set; }
    public int TrackNumber { get; set; }
    public int DiscNumber { get; set; }
    public int Year { get; set; }
    public string Date { get; set; }
    public int OriginalYear { get; set; }
    public int ReleaseYear { get; set; }
    public string ReleaseDate { get; set; }
    public long Size { get; set; }
    public string Suffix { get; set; }
    public double Duration { get; set; }
    public int BitRate { get; set; }
    public int SampleRate { get; set; }
    public int BitDepth { get; set; }
    public int Channels { get; set; }
    public string Genre { get; set; }
    public string OrderTitle { get; set; }
    public string OrderAlbumName { get; set; }
    public string OrderArtistName { get; set; }
    public string OrderAlbumArtistName { get; set; }
    public bool Compilation { get; set; }
    public string Lyrics { get; set; }
    public string ExplicitStatus { get; set; }

    public bool Missing { get; set; }
    public DateTime BirthTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TrackTagsEntity Tags { get; set; }
    public TrackParticipantsEntity Participants { get; set; }
}