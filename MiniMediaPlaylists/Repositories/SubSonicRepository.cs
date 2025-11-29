using Dapper;
using MiniMediaPlaylists.Models;
using Npgsql;

namespace MiniMediaPlaylists.Repositories;

public class SubSonicRepository
{
    private readonly string _connectionString;
    public SubSonicRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<Guid> UpsertServerAsync(string serverUrl)
    {
        string query = @"
            INSERT INTO playlists_subsonic_server (id, serverurl, lastsynctime)
            VALUES (@id, @serverurl, @lastsynctime)
            ON CONFLICT (ServerUrl)
            DO UPDATE SET lastsynctime = EXCLUDED.lastsynctime
            RETURNING Id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, 
            param: new
            {
                id = Guid.NewGuid(),
                serverUrl,
                lastsynctime = new DateTime(2000, 1, 1)
            });
    }
    public async Task SetLastSyncTimeAsync(Guid serverId)
    {
        string query = @"UPDATE playlists_subsonic_server SET lastsynctime = @lastsynctime WHERE id = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, new
        {
            id = serverId,
            lastsynctime = DateTime.Now
        });
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl, Guid snapshotId)
    {
        string query = @"select
	                         list.Id,
	                         list.Name,
	                         true as CanAddTracks,
	                         true as CanSortTracks
                         from playlists_subsonic_server pps 
                         join playlists_subsonic_playlist list on list.serverid = pps.id and list.snapshotId = @snapshotId
                         where pps.serverurl = @serverUrl";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericPlaylist>(query, 
            param: new
            {
                serverUrl,
                snapshotId
            })).ToList();
    }
    
    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId, Guid snapshotId)
    {
        string query = @"select
                             track.id as Id,
                             track.Artist as ArtistName,
                             track.Album as AlbumName,
                             track.title as Title,
                             track.UserRating as LikeRating,
                             track.playlist_sortorder AS PlaylistSortOrder
                         from playlists_subsonic_server pps 
                         join playlists_subsonic_playlist list on list.serverid = pps.id and list.snapshotId = @snapshotId
                         join playlists_subsonic_playlist_track track on track.serverid = pps.id and track.playlistid = list.id and track.snapshotId = @snapshotId
                         where pps.serverurl = @serverUrl
                         and list.id = @playlistId
                         order by track.playlist_sortorder asc";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                serverUrl,
                playlistId,
                snapshotId
            })).ToList();
    }
    
    public async Task<List<GenericTrack>> GetPlaylistTracksByNameAsync(string serverUrl, string name, Guid snapshotId)
    {
        string query = @"select
                             track.id as Id,
                             track.Artist as ArtistName,
                             track.Album as AlbumName,
                             track.title as Title,
                             track.UserRating as LikeRating
                         from playlists_subsonic_server pps 
                         join playlists_subsonic_playlist list on list.serverid = pps.id and list.snapshotId = @snapshotId
                         join playlists_subsonic_playlist_track track on track.serverid = pps.id and track.playlistid = list.id and track.snapshotId = @snapshotId
                         where pps.serverurl = @serverUrl
                         and list.name = @name";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                serverUrl,
                name,
                snapshotId
            })).ToList();
    }
}