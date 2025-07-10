CREATE TABLE public.playlists_spotify_owner (
    Id uuid NOT NULL,
    OwnerId text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT playlists_spotify_owner_pkey PRIMARY KEY (OwnerId)
);

CREATE TABLE public.playlists_spotify_playlist (
    Id text NOT NULL,
    OwnerId uuid NOT NULL,
    Href text NOT NULL,
    Name text NOT NULL,
    SnapshotId text NOT NULL,
    TrackCount int NOT NULL,
    Uri text NOT NULL,
    AddedAt timestamp NOT NULL,
    UpdatedAt timestamp NOT NULL,
    CONSTRAINT playlists_spotify_playlist_pkey PRIMARY KEY (Id, OwnerId)
);

CREATE TABLE public.playlists_spotify_playlist_track_artist (
    TrackId text NOT NULL,
    ArtistId text NOT NULL,
    AlbumId text NOT NULL,
    ArtistName text NOT NULL,
    Index int NOT NULL,
    CONSTRAINT playlists_spotify_playlist_track_artist_pkey PRIMARY KEY (TrackId, ArtistId, AlbumId)
);

CREATE TABLE public.playlists_spotify_playlist_track (
   Id text NOT NULL,
   PlayListId text NOT NULL,
   OwnerId uuid NOT NULL,
   AlbumType text NOT NULL,
   AlbumId text NOT NULL,
   AlbumName text NOT NULL,
   AlbumReleaseDate text NOT NULL,
   AlbumTotalTracks text NOT NULL,
   ArtistName text NOT NULL,
   Name text NOT NULL,
   AddedById text NOT NULL,
   AddedByType text NOT NULL,
   IsRemoved bool NOT NULL,
   AddedAt timestamp NOT NULL,
   CONSTRAINT playlists_spotify_playlist_track_pkey PRIMARY KEY (Id, PlayListId, OwnerId)
);

