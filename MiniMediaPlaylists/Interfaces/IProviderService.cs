using MiniMediaPlaylists.Models;
using SubSonicMedia.Responses.Playlists.Models;

namespace MiniMediaPlaylists.Interfaces;

public interface IProviderService
{
    Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl);
    Task<GenericPlaylist> CreatePlaylistAsync(string serverUrl, string name);
    Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId);
    Task<List<GenericTrack>> SearchTrackAsync(string serverUrl, string artist, string album, string title);
    Task<List<GenericTrack>> DeepSearchTrackAsync(string serverUrl, string artist, string album, string title);
    Task<bool> AddTrackToPlaylistAsync(string serverUrl, string playlistId, GenericTrack track);
    Task<bool> LikeTrackAsync(string serverUrl, GenericTrack track, float rating);
}