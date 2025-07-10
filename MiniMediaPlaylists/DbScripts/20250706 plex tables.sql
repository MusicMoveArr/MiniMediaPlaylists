CREATE TABLE public.playlists_plex_server (
    Id uuid NOT NULL,
    ServerUrl text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT playlist_plex_server_pkey PRIMARY KEY (ServerUrl)
);

CREATE TABLE public.playlists_plex_playlist (
    RatingKey text NOT NULL,
    ServerId uuid NOT NULL,
    Key text NOT NULL,
    Guid text NOT NULL,
    Type text NOT NULL,
    Title text NOT NULL,
    TitleSort text NOT NULL,
    Summary text NOT NULL,
    Smart bool NOT NULL,
    PlaylistType text NOT NULL,
    Composite text NOT NULL,
    Icon text NOT NULL,
    LastViewedAt timestamp NOT NULL,
    Duration int8 NOT NULL,
    LeafCount int8 NOT NULL,
    AddedAt timestamp NOT NULL,
    UpdatedAt timestamp NOT NULL,
    CONSTRAINT playlists_plex_playlist_pkey PRIMARY KEY (RatingKey, ServerId)
);

CREATE TABLE public.playlists_plex_playlist_track (
    RatingKey text NOT NULL,
    PlayListId text NOT NULL,
    ServerId uuid NOT NULL,
    Key text NOT NULL,
    Type text NOT NULL,
    Title text NOT NULL,
    Guid text NOT NULL,
    ParentStudio text NOT NULL,
    LibrarySectionTitle text NOT NULL,
    LibrarySectionID int NOT NULL,
    GrandparentTitle text NOT NULL,
    UserRating float NOT NULL,
    ParentTitle text NOT NULL,
    ParentYear int NOT NULL,
    MusicAnalysisVersion int NOT NULL,
    MediaId int8 NOT NULL,
    MediaPartId int8 NOT NULL,
    MediaPartKey text NOT NULL,
    MediaPartDuration int8 NOT NULL,
    MediaPartFile text NOT NULL,
    MediaPartContainer text NOT NULL,
    IsRemoved bool NOT NULL,
    LastViewedAt timestamp NOT NULL,
    LastRatedAt timestamp NOT NULL,
    AddedAt timestamp NOT NULL,
    CONSTRAINT playlists_plex_playlist_track_pkey PRIMARY KEY (RatingKey, PlayListId, ServerId)
);