CREATE TABLE public.playlists_snapshot (
    Id uuid NOT NULL,
    ServerId uuid NOT NULL,
    ServiceName text NOT NULL,
    IsComplete bool NOT NULL DEFAULT false,
    CreatedAt timestamp DEFAULT current_timestamp,
    CONSTRAINT playlists_snapshot_pkey PRIMARY KEY (Id)
);

alter table playlists_jellyfin_playlist add column SnapShotId uuid default null;
alter table playlists_jellyfin_playlist_track add column SnapShotId uuid default null;

alter table playlists_plex_playlist add column SnapShotId uuid default null;
alter table playlists_plex_playlist_track add column SnapShotId uuid default null;

alter table playlists_spotify_playlist drop column SnapShotId;
alter table playlists_spotify_playlist add column SnapShotId uuid default null;
alter table playlists_spotify_playlist_track add column SnapShotId uuid default null;

alter table playlists_subsonic_playlist add column SnapShotId uuid default null;
alter table playlists_subsonic_playlist_track add column SnapShotId uuid default null;

alter table playlists_tidal_playlist add column SnapShotId uuid default null;
alter table playlists_tidal_playlist_track add column SnapShotId uuid default null;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
DECLARE
    new_snapshotid uuid;
    serverid uuid;
BEGIN
    IF EXISTS (SELECT 1 FROM playlists_jellyfin_owner) THEN
        SELECT id INTO serverid FROM playlists_jellyfin_owner;
        SELECT gen_random_uuid() INTO new_snapshotid;
        INSERT INTO playlists_snapshot (Id, ServerId, ServiceName, CreatedAt, IsComplete)
                                VALUES  (new_snapshotid, serverid, 'Jellyfin', CURRENT_TIMESTAMP, true);
        
        UPDATE playlists_jellyfin_playlist SET SnapshotId = new_snapshotid;
        UPDATE playlists_jellyfin_playlist_track SET SnapshotId = new_snapshotid;
    END IF;
END $$;


DO $$
DECLARE
    new_snapshotid uuid;
    serverid uuid;
BEGIN
    IF EXISTS (SELECT 1 FROM playlists_plex_server) THEN
        SELECT id INTO serverid FROM playlists_plex_server;
        SELECT gen_random_uuid() INTO new_snapshotid;
        INSERT INTO playlists_snapshot (Id, ServerId, ServiceName, CreatedAt, IsComplete)
                                VALUES  (new_snapshotid, serverid, 'Plex', CURRENT_TIMESTAMP, true);
        
        UPDATE playlists_plex_playlist SET SnapshotId = new_snapshotid;
        UPDATE playlists_plex_playlist_track SET SnapshotId = new_snapshotid;
    END IF;
END $$;

DO $$
DECLARE
    new_snapshotid uuid;
    serverid uuid;
BEGIN
    IF EXISTS (SELECT 1 FROM playlists_spotify_owner) THEN
        SELECT id INTO serverid FROM playlists_spotify_owner;
        SELECT gen_random_uuid() INTO new_snapshotid;
        INSERT INTO playlists_snapshot (Id, ServerId, ServiceName, CreatedAt, IsComplete)
                                VALUES  (new_snapshotid, serverid, 'Spotify', CURRENT_TIMESTAMP, true);
        
        UPDATE playlists_spotify_playlist SET SnapshotId = new_snapshotid;
        UPDATE playlists_spotify_playlist_track SET SnapshotId = new_snapshotid;
    END IF;
END $$;

DO $$
DECLARE
    new_snapshotid uuid;
    serverid uuid;
BEGIN
    IF EXISTS (SELECT 1 FROM playlists_subsonic_server) THEN
        SELECT id INTO serverid FROM playlists_subsonic_server;
        SELECT gen_random_uuid() INTO new_snapshotid;
        INSERT INTO playlists_snapshot (Id, ServerId, ServiceName, CreatedAt, IsComplete)
                                VALUES  (new_snapshotid, serverid, 'Subsonic', CURRENT_TIMESTAMP, true);
        
        UPDATE playlists_subsonic_playlist SET SnapshotId = new_snapshotid;
        UPDATE playlists_subsonic_playlist_track SET SnapshotId = new_snapshotid;
    END IF;
END $$;

DO $$
DECLARE
    new_snapshotid uuid;
    serverid uuid;
BEGIN
    IF EXISTS (SELECT 1 FROM playlists_tidal_owner) THEN
        SELECT id INTO serverid FROM playlists_tidal_owner;
        SELECT gen_random_uuid() INTO new_snapshotid;
        INSERT INTO playlists_snapshot (Id, ServerId, ServiceName, CreatedAt, IsComplete)
                                VALUES  (new_snapshotid, serverid, 'Tidal', CURRENT_TIMESTAMP, true);
        
        UPDATE playlists_tidal_playlist SET SnapshotId = new_snapshotid;
        UPDATE playlists_tidal_playlist_track SET SnapshotId = new_snapshotid;
    END IF;
END $$;

--drop old constraints without SnapshotId
ALTER TABLE playlists_jellyfin_playlist DROP CONSTRAINT playlists_jellyfin_playlist_pkey;
ALTER TABLE playlists_jellyfin_playlist_track DROP CONSTRAINT playlists_jellyfin_playlist_track_pkey;

ALTER TABLE playlists_plex_playlist DROP CONSTRAINT playlists_plex_playlist_pkey;
ALTER TABLE playlists_plex_playlist_track DROP CONSTRAINT playlists_plex_playlist_track_pkey;

ALTER TABLE playlists_spotify_playlist DROP CONSTRAINT playlists_spotify_playlist_pkey;
ALTER TABLE playlists_spotify_playlist_track DROP CONSTRAINT playlists_spotify_playlist_track_pkey;

ALTER TABLE playlists_subsonic_playlist DROP CONSTRAINT playlists_subsonic_playlist_pkey;
ALTER TABLE playlists_subsonic_playlist_track DROP CONSTRAINT playlists_subsonic_playlist_track_pkey;

ALTER TABLE playlists_tidal_playlist DROP CONSTRAINT playlists_tidal_playlist_pkey;
ALTER TABLE playlists_tidal_playlist_track DROP CONSTRAINT playlists_tidal_playlist_track_pkey;

--create new constraints with SnapshotId
ALTER TABLE playlists_jellyfin_playlist ADD CONSTRAINT playlists_jellyfin_playlist_pkey PRIMARY KEY (Id, OwnerId, SnapShotId);
ALTER TABLE playlists_jellyfin_playlist_track ADD CONSTRAINT playlists_jellyfin_playlist_track_pkey PRIMARY KEY (Id, PlayListId, OwnerId, SnapShotId);

ALTER TABLE playlists_plex_playlist ADD CONSTRAINT playlists_plex_playlist_pkey PRIMARY KEY (RatingKey, ServerId, SnapShotId);
ALTER TABLE playlists_plex_playlist_track ADD CONSTRAINT playlists_plex_playlist_track_pkey PRIMARY KEY (RatingKey, PlayListId, ServerId, SnapShotId);

ALTER TABLE playlists_spotify_playlist ADD CONSTRAINT playlists_spotify_playlist_pkey PRIMARY KEY (Id, OwnerId, SnapShotId);
ALTER TABLE playlists_spotify_playlist_track ADD CONSTRAINT playlists_spotify_playlist_track_pkey PRIMARY KEY (Id, PlayListId, OwnerId, SnapShotId);

ALTER TABLE playlists_subsonic_playlist ADD CONSTRAINT playlists_subsonic_playlist_pkey PRIMARY KEY (Id, ServerId, SnapShotId);
ALTER TABLE playlists_subsonic_playlist_track ADD CONSTRAINT playlists_subsonic_playlist_track_pkey PRIMARY KEY (Id, PlayListId, ServerId, SnapShotId);

ALTER TABLE playlists_tidal_playlist ADD CONSTRAINT playlists_tidal_playlist_pkey PRIMARY KEY (Id, OwnerId, SnapShotId);
ALTER TABLE playlists_tidal_playlist_track ADD CONSTRAINT playlists_tidal_playlist_track_pkey PRIMARY KEY (Id, PlayListId, OwnerId, SnapShotId);