CREATE TABLE public.playlists_subsonic_server (
    Id uuid NOT NULL,
    ServerUrl text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT playlist_subsonic_server_pkey PRIMARY KEY (ServerUrl)
);

CREATE TABLE public.playlists_subsonic_playlist (
    Id text NOT NULL,
    ServerId uuid NOT NULL,
    ChangedAt timestamp NOT NULL,
    CreatedAt timestamp NOT NULL,
    Comment text NOT NULL,
    Duration int NOT NULL,
    Name text NOT NULL,
    Owner text NOT NULL,
    Public bool NOT NULL,
    SongCount int NOT NULL,
    CONSTRAINT playlists_subsonic_playlist_pkey PRIMARY KEY (Id, ServerId)
);

CREATE TABLE public.playlists_subsonic_playlist_track (
    Id text NOT NULL,
    PlayListId text NOT NULL,
    ServerId uuid NOT NULL,
    Album text NOT NULL,
    AlbumId text NOT NULL,
    Artist text NOT NULL,
    ArtistId text NOT NULL,
    Duration int NOT NULL,
    Title text NOT NULL,
    Path text NOT NULL,
    Size int8 NOT NULL,
    Year int NOT NULL,
    IsRemoved bool NOT NULL,
    AddedAt timestamp NOT NULL,
    CONSTRAINT playlists_subsonic_playlist_track_pkey PRIMARY KEY (Id, PlayListId, ServerId)
);