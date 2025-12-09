using MiniMediaPlaylists.Interfaces;

namespace MiniMediaPlaylists.Models;

public class GenericTrack
{
    public string Id { get; set; }
    public string ArtistName { get; set; }
    public string AlbumName { get; set; }
    public string Title { get; set; }
    public float LikeRating { get; set; }
    public string Uri { get; set; }
    public int PlaylistSortOrder { get; set; }
    public string PlaylistItemId { get; set; }
    public string? AlbumArtist { get; set; }

    public GenericTrack()
    {
        
    }
    
    public GenericTrack(string title, string artistName, string albumName)
    {
        this.Title = title;
        this.ArtistName = artistName;
        this.AlbumName = albumName;
    }
    public GenericTrack(string id, string title, string artistName, string albumName)
        :  this(title, artistName, albumName)
    {
        this.Id = id;
    }
    public GenericTrack(string id, string title, string artistName, string albumName, string uri)
        :  this(title, artistName, albumName)
    {
        this.Id = id;
        this.Uri = uri;
    }

    public GenericTrack(string title, string artistName, string albumName, int playlistSortOrder)
        : this(title, artistName, albumName)
    {
        this.PlaylistSortOrder = playlistSortOrder;
    }

    public GenericTrack(string id, string title, string artistName, string albumName, int playlistSortOrder, float likeRating)
        : this(id, title, artistName, albumName)
    {
        this.PlaylistSortOrder = playlistSortOrder;
        this.LikeRating = likeRating;
    }

    public GenericTrack(string id, string title, string artistName, string albumName, int playlistSortOrder, float likeRating, string playlistItemId)
        : this(id, title, artistName, albumName, playlistSortOrder, likeRating)
    {
        this.PlaylistItemId = playlistItemId;
    }

}