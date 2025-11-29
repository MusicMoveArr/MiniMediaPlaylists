namespace MiniMediaPlaylists.Models;

public class UpdatePlaylistTrackOrder
{
    public required string ToName { get; init; }
    public required GenericPlaylist ToPlaylist { get; init; }
    public required GenericTrack FromTrack { get; init; }
    public required GenericTrack ToTrack { get; init; }
    public required int NewPlaylistSortOrder { get; init; }
}