using Dapper;
using MiniMediaPlaylists.Interfaces;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Models.Plex;
using Npgsql;

namespace MiniMediaPlaylists.Repositories;

public class PlexRepository
{
    private readonly string _connectionString;
    public PlexRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<Guid> UpsertServerAsync(string serverUrl)
    {
        string query = @"
            INSERT INTO playlists_plex_server (id, serverurl, lastsynctime)
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
        string query = @"UPDATE playlists_plex_server SET lastsynctime = @lastsynctime WHERE id = @id";

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
	                         list.ratingkey as Id,
	                         list.Title as Name,
	                         CASE WHEN list.smart = true THEN false ELSE true end as CanAddTracks,
	                         CASE WHEN list.smart = true THEN false ELSE true end as CanSortTracks
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id and list.snapshotId = @snapshotId
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
                             track.ratingkey as Id,
                             track.GrandParentTitle as ArtistName,
                             track.ParentTitle as AlbumName,
                             track.title as Title,
                             track.UserRating as LikeRating,
                             track.playlist_sortorder AS PlaylistSortOrder,
                             track.playlist_itemid AS PlaylistItemId
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id and list.snapshotId = @snapshotId
                         join playlists_plex_playlist_track track on track.serverid = pps.id and track.playlistid = list.ratingkey and track.snapshotId = @snapshotId
                         where pps.serverurl = @serverUrl
                         and list.ratingkey = @playlistId
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
                             track.ratingkey as Id,
                             track.GrandParentTitle as ArtistName,
                             track.ParentTitle as AlbumName,
                             track.title as Title,
                             track.UserRating as LikeRating,
                             track.playlist_sortorder AS PlaylistSortOrder,
                             track.playlist_itemid AS PlaylistItemId
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id and list.snapshotId = @snapshotId
                         join playlists_plex_playlist_track track on track.serverid = pps.id and track.playlistid = list.ratingkey and track.snapshotId = @snapshotId
                         where pps.serverurl = @serverUrl
                         and list.title = @name";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                serverUrl,
                name,
                snapshotId
            })).ToList();
    }
    
    public async Task<bool> IsPlaylistUpdatedAsync(string serverUrl, string playlistId, long addedAt, long updatedAt, Guid snapshotId)
    {
        string query = @"select
	                         list.AddedAt,
	                         list.UpdatedAt
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id and list.snapshotId = @snapshotId
                         where pps.serverurl = @serverUrl
                         and list.ratingkey = @playlistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        var updatedModel = await conn.QueryFirstOrDefaultAsync<PlexPlaylistUpdatedModel>(query,
            param: new
            {
                serverUrl,
                playlistId,
                snapshotId
            });

        if (updatedModel == null)
        {
            return true;
        }
        
        return updatedModel.AddedAt == DateTimeOffset.FromUnixTimeSeconds(addedAt).Date && 
               updatedModel.UpdatedAt == DateTimeOffset.FromUnixTimeSeconds(updatedAt).Date;
    }
    public async Task<List<int>> GetLibrarySectionIdsAsync(string serverUrl, Guid snapshotId)
    {
        string query = @"select distinct
                             track.librarysectionid
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id and list.snapshotId = @snapshotId
                         join playlists_plex_playlist_track track on track.serverid = pps.id and track.playlistid = list.ratingkey and track.snapshotId = @snapshotId
                         where pps.serverurl = @serverUrl";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<int>(query, 
            param: new
            {
                serverUrl,
                snapshotId
            })).ToList();
    }
    
    public async Task DeleteSnapshotsAsync(List<Guid> snapshotIds)
    {
        string queryPlaylist = @"delete from playlists_plex_playlist 
                                 where snapshotid = ANY(@snapshotIds)";
        
        string queryPlaylistTracks = @"delete from playlists_plex_playlist_track 
                                       where snapshotid = ANY(@snapshotIds)";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(queryPlaylist,
            param: new
            {
                snapshotIds
            });
        await conn.ExecuteAsync(queryPlaylistTracks,
            param: new
            {
                snapshotIds
            });
    }
}