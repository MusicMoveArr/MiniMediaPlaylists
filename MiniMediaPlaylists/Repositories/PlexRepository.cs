using Dapper;
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
    
    public async Task<Guid> UpsertPlaylistAsync(
        PlaylistModel playlistModel,
        Guid serverId)
    {
        if (string.IsNullOrWhiteSpace(playlistModel.TitleSort))
        {
            playlistModel.TitleSort = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(playlistModel.Icon))
        {
            playlistModel.Icon = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(playlistModel.Composite))
        {
            playlistModel.Composite = string.Empty;
        }
        
        string query = @"
            INSERT INTO playlists_plex_playlist (RatingKey,
                                                 ServerId,
                                                 Key,
                                                 Guid,
                                                 Type,
                                                 Title,
                                                 TitleSort,
                                                 Summary,
                                                 Smart,
                                                 PlaylistType,
                                                 Composite,
                                                 Icon,
                                                 LastViewedAt,
                                                 Duration,
                                                 LeafCount,
                                                 AddedAt,
                                                 UpdatedAt)
            VALUES (@RatingKey,
                    @ServerId,
                    @Key,
                    @Guid,
                    @Type,
                    @Title,
                    @TitleSort,
                    @Summary,
                    @Smart,
                    @PlaylistType,
                    @Composite,
                    @Icon,
                    @LastViewedAt,
                    @Duration,
                    @LeafCount,
                    @AddedAt,
                    @UpdatedAt)
            ON CONFLICT (RatingKey, ServerId)
            DO UPDATE set
                Key = EXCLUDED.Key,
                Guid = EXCLUDED.Guid,
                Type = EXCLUDED.Type,
                Title = EXCLUDED.Title,
                TitleSort = EXCLUDED.TitleSort,
                Summary = EXCLUDED.Summary,
                Smart = EXCLUDED.Smart,
                PlaylistType = EXCLUDED.PlaylistType,
                Composite = EXCLUDED.Composite,
                Icon = EXCLUDED.Icon,
                LastViewedAt = EXCLUDED.LastViewedAt,
                Duration = EXCLUDED.Duration,
                LeafCount = EXCLUDED.LeafCount,
                AddedAt = EXCLUDED.AddedAt,
                UpdatedAt = EXCLUDED.UpdatedAt";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, 
            param: new
            {
                playlistModel.RatingKey,
                serverId,
                playlistModel.Key,
                playlistModel.Guid,
                playlistModel.Type,
                playlistModel.Title,
                playlistModel.TitleSort,
                playlistModel.Summary,
                playlistModel.Smart,
                playlistModel.PlaylistType,
                playlistModel.Composite,
                playlistModel.Icon,
                LastViewedAt = DateTimeOffset.FromUnixTimeSeconds(playlistModel.LastViewedAt).DateTime,
                playlistModel.Duration,
                playlistModel.LeafCount,
                AddedAt = DateTimeOffset.FromUnixTimeSeconds(playlistModel.AddedAt).DateTime,
                UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(playlistModel.UpdatedAt).DateTime
            });
    }
    
    
    public async Task UpsertPlaylistTrackAsync(
        PlexTrackModel trackModel,
        string playListId,
        Guid serverId)
    {
        if (string.IsNullOrWhiteSpace(trackModel.ParentStudio))
        {
            trackModel.ParentStudio = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(trackModel.MusicAnalysisVersion))
        {
            trackModel.MusicAnalysisVersion = "0";
        }
        if (string.IsNullOrWhiteSpace(trackModel.ParentTitle))
        {
            trackModel.ParentTitle = string.Empty;
        }
        
        string query = @"
            INSERT INTO playlists_plex_playlist_track (RatingKey,
                                                   PlayListId,
                                                   ServerId,
                                                   Key,
                                                   Type,
                                                   Title,
                                                   Guid,
                                                   ParentStudio,
                                                   LibrarySectionTitle,
                                                   LibrarySectionId,
                                                   GrandparentTitle,
                                                   UserRating,
                                                   ParentTitle,
                                                   ParentYear,
                                                   MusicAnalysisVersion,
                                                   MediaId,
                                                   MediaPartId,
                                                   MediaPartKey,
                                                   MediaPartDuration,
                                                   MediaPartFile,
                                                   MediaPartContainer,
                                                   IsRemoved,
                                                   LastViewedAt,
                                                   LastRatedAt,
                                                   AddedAt)
            VALUES (@RatingKey,
                    @PlayListId,
                    @ServerId,
                    @Key,
                    @Type,
                    @Title,
                    @Guid,
                    @ParentStudio,
                    @LibrarySectionTitle,
                    @LibrarySectionId,
                    @GrandparentTitle,
                    @UserRating,
                    @ParentTitle,
                    @ParentYear,
                    @MusicAnalysisVersion,
                    @MediaId,
                    @MediaPartId,
                    @MediaPartKey,
                    @MediaPartDuration,
                    @MediaPartFile,
                    @MediaPartContainer,
                    @IsRemoved,
                    @LastViewedAt,
                    @LastRatedAt,
                    @AddedAt)
            ON CONFLICT (RatingKey, PlayListId, ServerId)
            DO UPDATE set
                Key = EXCLUDED.Key,
                Type = EXCLUDED.Type,
                Title = EXCLUDED.Title,
                Guid = EXCLUDED.Guid,
                ParentStudio = EXCLUDED.ParentStudio,
                LibrarySectionTitle = EXCLUDED.LibrarySectionTitle,
                LibrarySectionId = EXCLUDED.LibrarySectionId,
                GrandparentTitle = EXCLUDED.GrandparentTitle,
                UserRating = EXCLUDED.UserRating,
                ParentTitle = EXCLUDED.ParentTitle,
                ParentYear = EXCLUDED.ParentYear,
                MusicAnalysisVersion = EXCLUDED.MusicAnalysisVersion,
                MediaId = EXCLUDED.MediaId,
                MediaPartId = EXCLUDED.MediaPartId,
                MediaPartKey = EXCLUDED.MediaPartKey,
                MediaPartDuration = EXCLUDED.MediaPartDuration,
                MediaPartFile = EXCLUDED.MediaPartFile,
                MediaPartContainer = EXCLUDED.MediaPartContainer,
                IsRemoved = EXCLUDED.IsRemoved,
                LastViewedAt = EXCLUDED.LastViewedAt,
                LastRatedAt = EXCLUDED.LastRatedAt,
                AddedAt = EXCLUDED.AddedAt";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, 
            param: new
            {
                serverId,
                playListId,
                trackModel.RatingKey,
                trackModel.Key,
                trackModel.Type,
                trackModel.Title,
                trackModel.Guid,
                trackModel.ParentStudio,
                trackModel.LibrarySectionTitle,
                trackModel.LibrarySectionId,
                trackModel.GrandparentTitle,
                trackModel.UserRating,
                trackModel.ParentTitle,
                trackModel.ParentYear,
                MusicAnalysisVersion = int.Parse(trackModel.MusicAnalysisVersion),
                MediaId = trackModel.Media.First().Id,
                MediaPartId = trackModel.Media.First().Part.First().Id,
                MediaPartKey = trackModel.Media.First().Part.First().Key,
                MediaPartDuration = trackModel.Media.First().Part.First().Duration,
                MediaPartFile = trackModel.Media.First().Part.First().File,
                MediaPartContainer = trackModel.Media.First().Part.First().Container,
                IsRemoved = false,
                LastViewedAt = DateTimeOffset.FromUnixTimeSeconds(trackModel.LastViewedAt).DateTime,
                LastRatedAt = DateTimeOffset.FromUnixTimeSeconds(trackModel.LastRatedAt).DateTime,
                AddedAt = DateTimeOffset.FromUnixTimeSeconds(trackModel.AddedAt).DateTime,
            });
    }
    
    public async Task<List<GenericPlaylist>> GetPlaylistsAsync(string serverUrl)
    {
        string query = @"select
	                         list.ratingkey as Id,
	                         list.Title as Name
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id 
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
                             track.ratingkey as Id,
                             track.GrandParentTitle as ArtistName,
                             track.ParentTitle as AlbumName,
                             track.title as Title,
                             track.UserRating as LikeRating
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id 
                         join playlists_plex_playlist_track track on track.serverid = pps.id and track.playlistid = list.ratingkey
                         where pps.serverurl = @serverUrl
                         and list.ratingkey = @playlistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<GenericTrack>(query, 
            param: new
            {
                serverUrl,
                playlistId
            })).ToList();
    }
    
    public async Task<bool> IsPlaylistUpdatedAsync(string serverUrl, string playlistId, long addedAt, long updatedAt)
    {
        string query = @"select
	                         list.AddedAt,
	                         list.UpdatedAt
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id 
                         where pps.serverurl = @serverUrl
                         and list.ratingkey = @playlistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        var updatedModel = await conn.QueryFirstOrDefaultAsync<PlexPlaylistUpdatedModel>(query,
            param: new
            {
                serverUrl,
                playlistId
            });

        if (updatedModel == null)
        {
            return true;
        }
        
        return updatedModel.AddedAt == DateTimeOffset.FromUnixTimeSeconds(addedAt).Date && 
               updatedModel.UpdatedAt == DateTimeOffset.FromUnixTimeSeconds(updatedAt).Date;
    }
    public async Task<List<int>> GetLibrarySectionIdsAsync(string serverUrl)
    {
        string query = @"select distinct
                             track.librarysectionid
                         from playlists_plex_server pps 
                         join playlists_plex_playlist list on list.serverid = pps.id 
                         join playlists_plex_playlist_track track on track.serverid = pps.id and track.playlistid = list.ratingkey
                         where pps.serverurl = @serverUrl";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<int>(query, 
            param: new
            {
                serverUrl
            })).ToList();
    }
}