using Dapper;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Models.Spotify;
using Npgsql;

namespace MiniMediaPlaylists.Repositories;

public class SpotifyRepository
{
    private readonly string _connectionString;
    public SpotifyRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<SpotifyOwnerModel> GetOwnerByNameAsync(string ownerName)
    {
        string query = @"SELECT * FROM playlists_spotify_owner
                         WHERE OwnerId = @ownerName";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QueryFirstAsync<SpotifyOwnerModel>(query, 
            param: new
            {
                ownerName
            });
    }
    public async Task SetLastSyncTimeAsync(Guid ownerId)
    {
        string query = @"UPDATE playlists_spotify_owner SET lastsynctime = @lastsynctime WHERE id = @id";

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
            INSERT INTO playlists_spotify_owner (id, OwnerId, lastsynctime, AuthClientId, AuthSecretId, AuthRefreshToken)
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
        int trackCount,
        string uri,
        DateTime addedAt, 
        DateTime updatedAt, 
        Guid snapshotId)
    {
        string query = @"
            INSERT INTO playlists_spotify_playlist (Id,
                                                   OwnerId,
                                                   Href,
                                                   Name,
                                                   TrackCount,
                                                   Uri,
                                                   AddedAt,
                                                   UpdatedAt,
                                                   SnapshotId)
            VALUES (@playlistId, @ownerId, @href, @name,
                    @trackCount, @uri, @addedAt, @updatedAt, @snapshotId)
            ON CONFLICT (Id, OwnerId, SnapShotId)
            DO UPDATE set
                href = EXCLUDED.href,
                name = EXCLUDED.name,
                trackCount = EXCLUDED.trackCount,
                uri = EXCLUDED.uri,
                addedAt = EXCLUDED.addedAt,
                updatedAt = EXCLUDED.updatedAt";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                playlistId,
                ownerId,
                href,
                name,
                trackCount,
                uri,
                addedAt,
                updatedAt,
                snapshotId
            });
    }
    
    
    public async Task UpsertPlaylistTrackArtistAsync(
        string trackId,
        string artistId, 
        string albumId,
        string artistName,
        int index)
    {
        string query = @"
            INSERT INTO playlists_spotify_playlist_track_artist (TrackId,
                                                   ArtistId,
                                                   AlbumId,
                                                   ArtistName,
                                                   Index)
            VALUES (@trackId, @artistId, @albumId, @artistName, @index)
            ON CONFLICT (TrackId, ArtistId, AlbumId)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                trackId,
                artistId,
                albumId,
                artistName,
                index
            });
    }
    
    public async Task UpsertPlaylistTrackAsync(
        string trackId,
        string playlistId,
        Guid ownerId, 
        string albumType,
        string albumId,
        string albumName,
        string albumReleaseDate,
        int albumTotalTracks,
        string artistName,
        string name, 
        string addedById, 
        string addedByType, 
        bool isRemoved, 
        DateTime addedAt, 
        Guid snapshotId,
        int playlistSortOrder)
    {
        string query = @"
            INSERT INTO playlists_spotify_playlist_track (Id,
                                                   PlayListId,
                                                   OwnerId,
                                                   AlbumType,
                                                   AlbumId,
                                                   AlbumName,
                                                   AlbumReleaseDate,
                                                   AlbumTotalTracks,
                                                   ArtistName,
                                                   Name,
                                                   AddedById,
                                                   AddedByType,
                                                   IsRemoved,
                                                   AddedAt,
                                                   SnapshotId,
                                                   playlist_sortorder)
            VALUES (@trackId, @playlistId, @ownerId, @albumType, @albumId,
                    @albumName, @albumReleaseDate, @albumTotalTracks, @artistName, @name,
                    @addedById, @addedByType, @isRemoved, @addedAt, @snapshotId, @playlistSortOrder)
            ON CONFLICT (Id, PlayListId, OwnerId, SnapShotId)
            DO UPDATE set
                AlbumType = EXCLUDED.AlbumType,
                AlbumId = EXCLUDED.AlbumId,
                AlbumName = EXCLUDED.AlbumName,
                AlbumReleaseDate = EXCLUDED.AlbumReleaseDate,
                AlbumTotalTracks = EXCLUDED.AlbumTotalTracks,
                ArtistName = EXCLUDED.ArtistName,
                Name = EXCLUDED.Name,
                AddedById = EXCLUDED.AddedById,
                AddedByType = EXCLUDED.AddedByType,
                IsRemoved = EXCLUDED.IsRemoved,
                AddedAt = EXCLUDED.AddedAt";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                trackId, 
                playlistId, 
                ownerId, 
                albumType, 
                albumId,
                albumName, 
                albumReleaseDate, 
                albumTotalTracks, 
                artistName, 
                name,
                addedById, 
                addedByType, 
                isRemoved, 
                addedAt,
                snapshotId,
                playlistSortOrder
            });
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string ownerId, Guid snapshotId)
    {
        string query = @"select
                             list.id,
                             list.Name,
                             true as CanAddTracks,
                             true as CanSortTracks
                         from playlists_spotify_owner ppo
                         join playlists_spotify_playlist list on list.ownerid = ppo.id and list.snapshotId = @snapshotId
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
                             track.AlbumName as AlbumName,
                             track.Name as Title,
                             track.playlist_sortorder AS PlaylistSortOrder
                         from playlists_spotify_owner ppo
                         join playlists_spotify_playlist list on list.ownerid = ppo.id and list.snapshotId = @snapshotId
                         join playlists_spotify_playlist_track track on track.ownerid = ppo.id and track.playlistid = list.id and track.snapshotId = @snapshotId
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
                             track.AlbumName as AlbumName,
                             track.Name as Title
                         from playlists_spotify_owner ppo
                         join playlists_spotify_playlist list on list.ownerid = ppo.id and list.snapshotId = @snapshotId
                         join playlists_spotify_playlist_track track on track.ownerid = ppo.id and track.playlistid = list.id and track.snapshotId = @snapshotId
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