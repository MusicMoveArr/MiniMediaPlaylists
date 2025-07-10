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
        string snapshotId,
        int trackCount,
        string uri,
        DateTime addedAt, 
        DateTime updatedAt)
    {
        string query = @"
            INSERT INTO playlists_spotify_playlist (Id,
                                                   OwnerId,
                                                   Href,
                                                   Name,
                                                   SnapshotId,
                                                   TrackCount,
                                                   Uri,
                                                   AddedAt,
                                                   UpdatedAt)
            VALUES (@playlistId, @ownerId, @href, @name, @snapshotId,
                    @trackCount, @uri, @addedAt, @updatedAt)
            ON CONFLICT (Id, OwnerId)
            DO UPDATE set
                href = EXCLUDED.href,
                name = EXCLUDED.name,
                snapshotId = EXCLUDED.snapshotId,
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
                snapshotId,
                trackCount,
                uri,
                addedAt,
                updatedAt
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
        DateTime addedAt)
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
                                                   AddedAt)
            VALUES (@trackId, @playlistId, @ownerId, @albumType, @albumId,
                    @albumName, @albumReleaseDate, @albumTotalTracks, @artistName, @name,
                    @addedById, @addedByType, @isRemoved, @addedAt)
            ON CONFLICT (Id, PlayListId, OwnerId)
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
                addedAt
            });
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string ownerId)
    {
        string query = @"select
                             list.id,
                             list.Name
                         from playlists_spotify_owner ppo
                         join playlists_spotify_playlist list on list.ownerid = ppo.id 
                         where ppo.ownerid = @ownerId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericPlaylist>(query, 
            param: new
            {
                ownerId
            })).ToList();
    }
    
    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string ownerId, string playlistId)
    {
        string query = @"select
                             track.id as Id,
                             track.ArtistName as ArtistName,
                             track.AlbumName as AlbumName,
                             track.Name as Title
                         from playlists_spotify_owner ppo
                         join playlists_spotify_playlist list on list.ownerid = ppo.id 
                         join playlists_spotify_playlist_track track on track.ownerid = ppo.id and track.playlistid = list.id
                         where ppo.ownerid = @ownerId
                         and list.id = @playlistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                ownerId,
                playlistId
            })).ToList();
    }
}