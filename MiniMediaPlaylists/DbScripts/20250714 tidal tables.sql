CREATE TABLE public.playlists_tidal_owner (
    Id uuid NOT NULL,
    OwnerId text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    AuthClientId text NOT NULL,
    AuthSecretId text NOT NULL,
    AuthRefreshToken text NOT NULL,
    CONSTRAINT playlists_tidal_owner_pkey PRIMARY KEY (OwnerId)
);

CREATE TABLE public.playlists_tidal_playlist (
    Id text NOT NULL,
    OwnerId uuid NOT NULL,
    Href text NOT NULL,
    Name text NOT NULL,
    Description text NOT NULL,
    Bounded bool NOT NULL,
    Duration text NOT NULL,
    NumberOfItems int NOT NULL,
    CreatedAt timestamp NOT NULL,
    LastModifiedAt timestamp NOT NULL,
    Privacy text NOT NULL,
    AccessType text NOT NULL,
    PlaylistType text NOT NULL,
    CONSTRAINT playlists_tidal_playlist_pkey PRIMARY KEY (Id, OwnerId)
);

CREATE TABLE public.playlists_tidal_playlist_track (
   Id text NOT NULL,
   PlayListId text NOT NULL,
   OwnerId uuid NOT NULL,
   MetaItemId text NOT NULL,
   Title text NOT NULL,
   ISRC text NOT NULL,
   Explicit bool NOT NULL,
   AlbumTitle text NOT NULL,
   AlbumReleaseDate text NOT NULL,
   AlbumBarcodeId text NOT NULL,
   AlbumExplicit bool NOT NULL,
   AlbumType text NOT NULL,
   ArtistName text NOT NULL,
   IsRemoved bool NOT NULL,
   AddedAt timestamp NOT NULL,
   CONSTRAINT playlists_tidal_playlist_track_pkey PRIMARY KEY (Id, PlayListId, OwnerId)
);

