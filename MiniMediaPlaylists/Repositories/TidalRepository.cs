using Dapper;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Models.Tidal;
using Npgsql;

namespace MiniMediaPlaylists.Repositories;

public class TidalRepository
{
    private readonly string _connectionString;
    public TidalRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<TidalOwnerModel?> GetOwnerByNameAsync(string ownerName)
    {
        string query = @"SELECT * FROM playlists_tidal_owner
                         WHERE OwnerId = @ownerName";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QueryFirstOrDefaultAsync<TidalOwnerModel>(query, 
            param: new
            {
                ownerName
            });
    }
    public async Task SetLastSyncTimeAsync(Guid ownerId)
    {
        string query = @"UPDATE playlists_tidal_owner SET lastsynctime = @lastsynctime WHERE id = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, new
        {
            id = ownerId,
            lastsynctime = DateTime.Now
        });
    }
    
    public async Task<Guid> UpsertOwnerAsync(string ownerId, string authClientId, string authSecretId, string authRefreshToken)
    {
        string query = @"
            INSERT INTO playlists_tidal_owner (id, OwnerId, lastsynctime, AuthClientId, AuthSecretId, AuthRefreshToken)
            VALUES (@id, @ownerId, @lastsynctime, @authClientId, @authSecretId, @authRefreshToken)
            ON CONFLICT (OwnerId)
            DO UPDATE SET 
                lastsynctime = EXCLUDED.lastsynctime,
                AuthClientId = EXCLUDED.AuthClientId,
                AuthSecretId = EXCLUDED.AuthSecretId,
                AuthRefreshToken = EXCLUDED.AuthRefreshToken
            RETURNING Id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, 
            param: new
            {
                id = Guid.NewGuid(),
                ownerId,
                lastsynctime = new DateTime(2000, 1, 1),
                authClientId,
                authSecretId,
                authRefreshToken
            });
    }
    
    public async Task UpsertPlaylistAsync(
        string playlistId,
        Guid ownerId, 
        string href,
        string name,
        string description,
        bool bounded,
        string duration,
        int numberOfItems,
        DateTime createdAt, 
        DateTime lastModifiedAt,
        string privacy,
        string accessType,
        string playlistType, 
        Guid snapshotId)
    {
        string query = @"
            INSERT INTO playlists_tidal_playlist (Id,
                                                  OwnerId,
                                                  Href,
                                                  Name,
                                                  Description,
                                                  Bounded,
                                                  Duration,
                                                  NumberOfItems,
                                                  CreatedAt,
                                                  LastModifiedAt,
                                                  Privacy,
                                                  AccessType,
                                                  PlaylistType,
                                                  SnapshotId)
            VALUES (@playlistId, @ownerId, @href, @name, @description,
                    @bounded, @duration, @numberOfItems, @createdAt,
                    @lastModifiedAt, @privacy, @accessType, @playlistType, @snapshotId)
            ON CONFLICT (Id, OwnerId, SnapShotId)
            DO UPDATE set
                Href = EXCLUDED.Href,
                name = EXCLUDED.name,
                Description = EXCLUDED.Description,
                Bounded = EXCLUDED.Bounded,
                Duration = EXCLUDED.Duration,
                CreatedAt = EXCLUDED.CreatedAt,
                LastModifiedAt = EXCLUDED.LastModifiedAt,
                Privacy = EXCLUDED.Privacy,
                AccessType = EXCLUDED.AccessType,
                PlaylistType = EXCLUDED.PlaylistType";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                playlistId,
                ownerId,
                href,
                name,
                description,
                bounded,
                duration,
                numberOfItems,
                createdAt,
                lastModifiedAt,
                privacy,
                accessType,
                playlistType,
                snapshotId
            });
    }
    
    public async Task UpsertPlaylistTrackAsync(
        string trackId,
        string playlistId,
        Guid ownerId, 
        string metaItemId,
        string title,
        string isrc,
        bool trackExplicit,
        string albumTitle,
        string albumReleaseDate,
        string albumBarcodeId, 
        bool albumExplicit, 
        string albumType, 
        string artistName, 
        bool isRemoved, 
        DateTime addedAt, 
        Guid snapshotId)
    {
        string query = @"
            INSERT INTO playlists_tidal_playlist_track (Id,
                                                   PlayListId,
                                                   OwnerId,
                                                   MetaItemId,
                                                   Title,
                                                   ISRC,
                                                   Explicit,
                                                   AlbumTitle,
                                                   AlbumReleaseDate,
                                                   AlbumBarcodeId,
                                                   AlbumExplicit,
                                                   AlbumType,
                                                   ArtistName,
                                                   IsRemoved,
                                                   AddedAt,
                                                   SnapshotId)
            VALUES (@trackId, @playlistId, @ownerId, @metaItemId, @title,
                    @isrc, @trackExplicit, @albumTitle, @albumReleaseDate, @albumBarcodeId,
                    @albumExplicit, @albumType, @artistName, @isRemoved, @addedAt, @snapshotId)
            ON CONFLICT (Id, PlayListId, OwnerId, SnapShotId)
            DO UPDATE set
                MetaItemId = EXCLUDED.MetaItemId,
                Title = EXCLUDED.Title,
                ISRC = EXCLUDED.ISRC,
                Explicit = EXCLUDED.Explicit,
                AlbumTitle = EXCLUDED.AlbumTitle,
                AlbumReleaseDate = EXCLUDED.AlbumReleaseDate,
                AlbumBarcodeId = EXCLUDED.AlbumBarcodeId,
                AlbumExplicit = EXCLUDED.AlbumExplicit,
                AlbumType = EXCLUDED.AlbumType,
                ArtistName = EXCLUDED.ArtistName,
                IsRemoved = EXCLUDED.IsRemoved,
                AddedAt = EXCLUDED.AddedAt";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                trackId, 
                playlistId, 
                ownerId, 
                metaItemId, 
                title,
                isrc, 
                trackExplicit, 
                albumTitle, 
                albumReleaseDate, 
                albumBarcodeId,
                albumExplicit, 
                albumType, 
                artistName, 
                isRemoved, 
                addedAt,
                snapshotId
            });
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string ownerId, Guid snapshotId)
    {
        string query = @"select
                             list.id,
                             list.Name,
                             true as CanAddTracks,
                             true as CanSortTracks
                         from playlists_tidal_owner ppo
                         join playlists_tidal_playlist list on list.ownerid = ppo.id and list.snapshotId = @snapshotId
                         where ppo.ownerid = @ownerId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericPlaylist>(query, 
            param: new
            {
                ownerId,
                snapshotId
            })).ToList();
    }
    
    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string ownerId, string playlistId, Guid snapshotId)
    {
        string query = @"select
                             track.id as Id,
                             track.ArtistName as ArtistName,
                             track.AlbumTitle as AlbumName,
                             track.Title as Title
                         from playlists_tidal_owner ppo
                         join playlists_tidal_playlist list on list.ownerid = ppo.id and list.snapshotId = @snapshotId
                         join playlists_tidal_playlist_track track on track.ownerid = ppo.id and track.playlistid = list.id and track.snapshotId = @snapshotId
                         where ppo.ownerid = @ownerId
                         and list.id = @playlistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                ownerId,
                playlistId,
                snapshotId
            })).ToList();
    }
    
    public async Task<List<GenericTrack>> GetPlaylistTracksByNameAsync(string ownerId, string name, Guid snapshotId)
    {
        string query = @"select
                             track.id as Id,
                             track.ArtistName as ArtistName,
                             track.AlbumTitle as AlbumName,
                             track.Title as Title
                         from playlists_tidal_owner ppo
                         join playlists_tidal_playlist list on list.ownerid = ppo.id and list.snapshotId = @snapshotId
                         join playlists_tidal_playlist_track track on track.ownerid = ppo.id and track.playlistid = list.id and track.snapshotId = @snapshotId
                         where ppo.ownerid = @ownerId
                         and list.name = @name";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                ownerId,
                name,
                snapshotId
            })).ToList();
    }
}