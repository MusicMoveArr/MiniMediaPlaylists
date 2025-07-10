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
    
    public async Task<Guid> UpsertPlaylistAsync(
        string playlistId,
        Guid serverId, 
        DateTime changedAt,
        DateTime createdAt,
        string comment,
        int duration,
        string name,
        string owner, 
        bool isPublic,
        int songCount)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            comment = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            name = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(owner))
        {
            owner = string.Empty;
        }
        
        string query = @"
            INSERT INTO playlists_subsonic_playlist (Id,
                                                   ServerId,
                                                   ChangedAt,
                                                   CreatedAt,
                                                   Comment,
                                                   Duration,
                                                   Name,
                                                   Owner,
                                                   Public,
                                                   SongCount)
            VALUES (@playlistId, @serverId, @changedAt, @createdAt, @comment,
                    @duration, @name, @owner, @isPublic, @songCount)
            ON CONFLICT (Id, ServerId)
            DO UPDATE set
                ChangedAt = EXCLUDED.ChangedAt,
                CreatedAt = EXCLUDED.CreatedAt,
                Comment = EXCLUDED.Comment,
                Duration = EXCLUDED.Duration,
                Name = EXCLUDED.Name,
                Owner = EXCLUDED.Owner,
                Public = EXCLUDED.Public,
                SongCount = EXCLUDED.SongCount";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, 
            param: new
            {
                playlistId,
                serverId,
                changedAt,
                createdAt,
                comment,
                duration,
                name,
                owner,
                isPublic,
                songCount
            });
    }
    
    
    public async Task UpsertPlaylistTrackAsync(
        string trackId,
        Guid serverId, 
        string playlistId, 
        string album,
        string albumId,
        string artist,
        string artistId,
        int duration, 
        string title,
        string path,
        long size,
        int year,
        DateTime addedAt)
    {
        if (string.IsNullOrWhiteSpace(album))
        {
            album = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(albumId))
        {
            albumId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(artist))
        {
            artist = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(artistId))
        {
            artistId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            title = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            path = string.Empty;
        }
        
        
        
        
        string query = @"
            INSERT INTO playlists_subsonic_playlist_track (Id,
                                                   PlayListId,
                                                   ServerId,
                                                   Album,
                                                   AlbumId,
                                                   Artist,
                                                   ArtistId,
                                                   Duration,
                                                   Title,
                                                   Path,
                                                   Size,
                                                   Year,
                                                   IsRemoved,
                                                   AddedAt)
            VALUES (@trackId, @playlistId, @serverId, @album, @albumId, @artist, @artistId,
                    @duration, @title, @path, @size, @year, @isRemoved, @addedAt)
            ON CONFLICT (Id, PlayListId, ServerId)
            DO UPDATE set
                album = EXCLUDED.album,
                albumId = EXCLUDED.albumId,
                artist = EXCLUDED.artist,
                artistId = EXCLUDED.artistId,
                Duration = EXCLUDED.Duration,
                title = EXCLUDED.title,
                path = EXCLUDED.path,
                size = EXCLUDED.size,
                year = EXCLUDED.year,
                addedAt = EXCLUDED.addedAt";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                trackId,
                playlistId,
                serverId,
                album,
                albumId,
                artist,
                artistId,
                duration,
                title,
                path,
                size,
                year,
                isRemoved = false,
                addedAt
            });
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl)
    {
        string query = @"select
	                         list.Id,
	                         list.Name
                         from playlists_subsonic_server pps 
                         join playlists_subsonic_playlist list on list.serverid = pps.id 
                         where pps.serverurl = @serverUrl";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericPlaylist>(query, 
            param: new
            {
                serverUrl
            })).ToList();
    }
    
    public async Task<List<GenericTrack>> GetPlaylistTracksAsync(string serverUrl, string playlistId)
    {
        string query = @"select
                             track.id as Id,
                             track.Artist as ArtistName,
                             track.Album as AlbumName,
                             track.title as Title
                         from playlists_subsonic_server pps 
                         join playlists_subsonic_playlist list on list.serverid = pps.id 
                         join playlists_subsonic_playlist_track track on track.serverid = pps.id and track.playlistid = list.id
                         where pps.serverurl = @serverUrl
                         and list.id = @playlistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                serverUrl,
                playlistId
            })).ToList();
    }
}