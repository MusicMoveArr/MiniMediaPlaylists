using Dapper;
using MiniMediaPlaylists.Models;
using MiniMediaPlaylists.Models.Jellyfin;
using MiniMediaPlaylists.Models.Tidal;
using Npgsql;

namespace MiniMediaPlaylists.Repositories;

public class JellyfinRepository
{
    private readonly string _connectionString;
    public JellyfinRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<JellyfinOwnerModel?> GetOwnerByNameAsync(string username, string serverUrl)
    {
        string query = @"SELECT * FROM playlists_jellyfin_owner
                         WHERE Username = @username and ServerUrl = @serverUrl";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QueryFirstOrDefaultAsync<JellyfinOwnerModel>(query, 
            param: new
            {
                username,
                serverUrl
            });
    }
    public async Task SetLastSyncTimeAsync(Guid ownerId)
    {
        string query = @"UPDATE playlists_jellyfin_owner SET lastsynctime = @lastsynctime WHERE id = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, new
        {
            id = ownerId,
            lastsynctime = DateTime.Now
        });
    }
    
    public async Task<Guid> UpsertOwnerAsync(string username, string jellyfinUserId, string accessToken, string serverUrl)
    {
        string query = @"
            INSERT INTO playlists_jellyfin_owner (id, Username, JellyfinUserId, AccessToken, ServerUrl, lastsynctime)
            VALUES (@id, @username, @jellyfinUserId, @accessToken, @serverUrl, @lastsynctime)
            ON CONFLICT (JellyfinUserId)
            DO UPDATE SET 
                AccessToken = EXCLUDED.AccessToken
            RETURNING Id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, 
            param: new
            {
                id = Guid.NewGuid(),
                username,
                jellyfinUserId,
                accessToken,
                serverUrl,
                lastsynctime = new DateTime(2000, 1, 1),
            });
    }
    
    public async Task UpsertPlaylistAsync(
        string playlistId,
        Guid ownerId, 
        string name,
        string serverId,
        string channelId,
        bool isFolder,
        string userDataKey,
        string mediaType,
        string locationType)
    {
        string query = @"
            INSERT INTO playlists_jellyfin_playlist (Id,
                                                  OwnerId,
                                                  Name,
                                                  ServerId,
                                                  ChannelId,
                                                  IsFolder,
                                                  UserDataKey,
                                                  MediaType,
                                                  LocationType)
            VALUES (@playlistId, @ownerId, @name, @serverId,
                    @channelId, @isFolder, @userDataKey, @mediaType,
                    @locationType)
            ON CONFLICT (Id, OwnerId)
            DO UPDATE set
                name = EXCLUDED.name,
                ServerId = EXCLUDED.ServerId,
                ChannelId = EXCLUDED.ChannelId,
                IsFolder = EXCLUDED.IsFolder,
                UserDataKey = EXCLUDED.UserDataKey,
                MediaType = EXCLUDED.MediaType,
                LocationType = EXCLUDED.LocationType";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                playlistId,
                ownerId,
                name,
                serverId,
                channelId,
                isFolder,
                userDataKey,
                mediaType,
                locationType
            });
    }
    
    public async Task UpsertPlaylistTrackAsync(
        string trackId,
        string playlistId,
        Guid ownerId, 
        string title,
        string? artist,
        string albumArtist,
        string album,
        string playListItemId,
        string container,
        DateTime premiereDate,
        string channelId,
        int productionYear,
        int indexNumber,
        bool isFolder,
        string userDataKey,
        bool userDataIsFavorite,
        string mediaType,
        string locationType,
        bool isRemoved,
        DateTime addedAt)
    {
        string query = @"
            INSERT INTO playlists_jellyfin_playlist_track (Id,
                                                   PlayListId,
                                                   OwnerId,
                                                   Title,
                                                   Artist,
                                                   AlbumArtist,
                                                   Album,
                                                   PlayListItemId,
                                                   Container,
                                                   PremiereDate,
                                                   ChannelId,
                                                   ProductionYear,
                                                   IndexNumber,
                                                   IsFolder,
                                                   UserDataKey,
                                                   UserDataIsFavorite,
                                                   MediaType,
                                                   LocationType,
                                                   IsRemoved,
                                                   AddedAt)
            VALUES (@trackId, @playlistId, @ownerId, @title, @artist, @albumArtist,
                @album, @playListItemId, @container, @premiereDate, @channelId,
                @productionYear, @indexNumber,  @isFolder, @userDataKey, @userDataIsFavorite,
                @mediaType, @locationType, @isRemoved, @addedAt)
            ON CONFLICT (Id, PlayListId, OwnerId)
            DO UPDATE set
                Title = EXCLUDED.Title,
                artist = EXCLUDED.artist,
                albumArtist = EXCLUDED.albumArtist,
                album = EXCLUDED.album,
                playListItemId = EXCLUDED.playListItemId,
                container = EXCLUDED.container,
                premiereDate = EXCLUDED.premiereDate,
                channelId = EXCLUDED.channelId,
                productionYear = EXCLUDED.productionYear,
                indexNumber = EXCLUDED.indexNumber,
                isFolder = EXCLUDED.isFolder,
                userDataKey = EXCLUDED.userDataKey,
                UserDataIsFavorite = EXCLUDED.UserDataIsFavorite,
                mediaType = EXCLUDED.mediaType,
                locationType = EXCLUDED.locationType,
                isRemoved = EXCLUDED.isRemoved,
                addedAt = EXCLUDED.addedAt";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                trackId,
                playlistId,
                ownerId, 
                title,
                artist,
                albumArtist,
                album,
                playListItemId,
                container,
                premiereDate,
                channelId,
                productionYear,
                indexNumber, 
                isFolder,
                userDataKey,
                userDataIsFavorite,
                mediaType,
                locationType,
                isRemoved,
                addedAt
            });
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string username)
    {
        string query = @"select
                             list.id,
                             list.Name
                         from playlists_jellyfin_owner ppo
                         join playlists_jellyfin_playlist list on list.ownerid = ppo.id 
                         where ppo.username = @username";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericPlaylist>(query, 
            param: new
            {
                username
            })).ToList();
    }
    
    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string username, string playlistId)
    {
        string query = @"select
                             track.id as Id,
                             track.Artist as ArtistName,
                             track.Album as AlbumName,
                             track.Title as Title
                         from playlists_jellyfin_owner ppo
                         join playlists_jellyfin_playlist list on list.ownerid = ppo.id 
                         join playlists_jellyfin_playlist_track track on track.ownerid = ppo.id and track.playlistid = list.id
                         where ppo.username = @username
                         and list.id = @playlistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                username,
                playlistId
            })).ToList();
    }
}