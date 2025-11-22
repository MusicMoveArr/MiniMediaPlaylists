using Dapper;
using Npgsql;

namespace MiniMediaPlaylists.Repositories;

public class SnapshotRepository
{
    private readonly string _connectionString;
    public SnapshotRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<Guid> CreateSnapshotAsync(Guid serverId, string serviceName)
    {
        string query = @"INSERT INTO playlists_snapshot (Id, ServerId, ServiceName, CreatedAt)
                                VALUES  (@id, @serverId, @serviceName, CURRENT_TIMESTAMP);";

        await using var conn = new NpgsqlConnection(_connectionString);
        Guid snapshotId = Guid.NewGuid();

        await conn.ExecuteAsync(query, 
            param: new
            {
                id = snapshotId,
                serverId,
                serviceName
            });
        return snapshotId;
    }
    
    public async Task SetSnapshotCompleteAsync(Guid snapshotId)
    {
        string query = @"UPDATE playlists_snapshot SET IsComplete = true WHERE id = @snapshotId";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, new
        {
            snapshotId
        });
    }
    
    public async Task<Guid?> GetLastCompleteTransactionJellyfinAsync(string ownerId)
    {
        string query = @"SELECT snapshot.id FROM playlists_snapshot snapshot
                         JOIN playlists_jellyfin_owner owner on owner.id = snapshot.serverId
                         WHERE 
                             snapshot.IsComplete = true
                             and owner.ServerUrl = @ownerId
                         ORDER BY snapshot.CreatedAt desc";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QueryFirstOrDefaultAsync<Guid>(query, 
            param: new
            {
                ownerId
            });
    }
    
    public async Task<Guid?> GetLastCompleteTransactionPlexAsync(string serverName)
    {
        string query = @"SELECT snapshot.id FROM playlists_snapshot snapshot
                         JOIN playlists_plex_server server on server.id = snapshot.serverId
                         WHERE 
                             snapshot.IsComplete = true
                             and server.ServerUrl = @serverName
                         ORDER BY snapshot.CreatedAt desc";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QueryFirstOrDefaultAsync<Guid>(query, 
            param: new
            {
                serverName
            });
    }
    
    public async Task<Guid?> GetLastCompleteTransactionSpotifyAsync(string ownerId)
    {
        string query = @"SELECT snapshot.id FROM playlists_snapshot snapshot
                         JOIN playlists_spotify_owner owner on owner.id = snapshot.serverId
                         WHERE 
                             snapshot.IsComplete = true
                             and owner.OwnerId = @ownerId
                         ORDER BY snapshot.CreatedAt desc";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QueryFirstOrDefaultAsync<Guid>(query, 
            param: new
            {
                ownerId
            });
    }
    
    public async Task<Guid?> GetLastCompleteTransactionSubsonicAsync(string serverName)
    {
        string query = @"SELECT snapshot.id FROM playlists_snapshot snapshot
                         JOIN playlists_subsonic_server server on server.id = snapshot.serverId
                         WHERE 
                             snapshot.IsComplete = true
                             and server.ServerUrl = @serverName
                         ORDER BY snapshot.CreatedAt desc";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QueryFirstOrDefaultAsync<Guid>(query, 
            param: new
            {
                serverName
            });
    }
    
    public async Task<Guid?> GetLastCompleteTransactionTidalAsync(string ownerId)
    {
        string query = @"SELECT snapshot.id FROM playlists_snapshot snapshot
                         JOIN playlists_tidal_owner owner on owner.id = snapshot.serverId
                         WHERE 
                             snapshot.IsComplete = true
                             and owner.OwnerId = @ownerId
                         ORDER BY snapshot.CreatedAt desc";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QueryFirstOrDefaultAsync<Guid>(query, 
            param: new
            {
                ownerId
            });
    }
}