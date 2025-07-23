CREATE TABLE public.playlists_jellyfin_owner (
    Id uuid NOT NULL,
    Username text NOT NULL,
    JellyfinUserId text NOT NULL,
    AccessToken text NOT NULL,
    ServerUrl text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT playlists_jellyfin_owner_pkey PRIMARY KEY (JellyfinUserId)
);

CREATE TABLE public.playlists_jellyfin_playlist (
    Id text NOT NULL,
    OwnerId uuid NOT NULL,
    Name text NOT NULL,
    ServerId text NOT NULL,
    ChannelId text NOT NULL,
    IsFolder bool NOT NULL,
    UserDataKey text NOT NULL,
    MediaType text NOT NULL,
    LocationType text NOT NULL,
    CONSTRAINT playlists_jellyfin_playlist_pkey PRIMARY KEY (Id, OwnerId)
);

CREATE TABLE public.playlists_jellyfin_playlist_track (
   Id text NOT NULL,
   PlayListId text NOT NULL,
   OwnerId uuid NOT NULL,
   Title text NOT NULL,
   Artist text NOT NULL,
   AlbumArtist text NOT NULL,
   Album text NOT NULL,
   PlayListItemId text NOT NULL,
   Container text NOT NULL,
   PremiereDate timestamp NOT NULL,
   ChannelId text NOT NULL,
   ProductionYear int NOT NULL,
   IndexNumber int NOT NULL,
   IsFolder bool NOT NULL,
   UserDataKey text NOT NULL,
   UserDataIsFavorite bool NOT NULL,
   MediaType text NOT NULL,
   LocationType text NOT NULL,
   IsRemoved bool NOT NULL,
   AddedAt timestamp NOT NULL,
   CONSTRAINT playlists_jellyfin_playlist_track_pkey PRIMARY KEY (Id, PlayListId, OwnerId)
);

