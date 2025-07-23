# MiniMedia Playlists
MiniMedia's Playlists - Cross-Platform/Provider playlist synchronization

Synchronize the way you want it easily from Spotify To Navidrome, Plex To Spotify whatever your weird combination shall be

No other tool could do this yet so some one had to make this...

As a "extra feature" you can sync to yourself to "restore" a "backup" in qoutes because by creating weird combinations you can even sync to the same service, at the bottom of the page is a restore example

Loving the work I do? buy me a coffee https://buymeacoffee.com/musicmovearr

# Roadmap
- [ ] More streaming providers support like Deezer, Tidal etc
- [ ] PreConfiguration Rules files
- [ ] Playlist versioning (Snapshots)

# Supported services
1. Spotify
2. Plex
3. SubSonic (includes Navidrome)
4. Tidal
5. Jellyfin

# Features
1. Postgres support
2. Store the playlists locally as a "backup"
3. Cross-Sync between the providers e.g. Spotify <-> Plex <-> SubSonic/Navidrome
4. Cross-Sync the liked songs + the song ratings

# Commands
1. Sync - Sync playlists between 2 services
2. PullPlex - Pull all your Plex playlists
3. PullSpotify - Pull all your spotify playlists
4. PullSubsonic - Pull all your SubSonic playlists
5. PullTidal - Pull all your Tidal playlists
6. PullJellyfin  - Pull all your Jellyfin playlists

# Docker-Compose example
This example will pull the plex playlists every 6 hours and playlists that have over 5000 tracks won't get pulled
```
services:
  minimediaplaylists:
    image: musicmovearr/minimediaplaylists:latest
    container_name: minimediaplaylists
    restart: unless-stopped
    environment:
      - PUID=1000
      - PGID=1000
      - COMMAND=pullplex
      - PULLPLEX_URL=http://xxxxxxx/
      - PULLPLEX_TOKEN=xxxxxxxx
      - PULLPLEX_LIMIT=5000
      - CONNECTIONSTRING=Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia
      - CRON=0 0 */6 ? * *
```

# Description of arguments
| Command | Longname Argument  | Description | Example |
| ------------- | ------------- | ------------- | ------------- |
| PullPlex | --connection-string | ConnectionString for Postgres database. | Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia |
| PullPlex | --url | Plex server url. | http://xxxxxxx/ |
| PullPlex | --token | Plex token for authentication. | xxxxxxx |
| PullPlex | --track-limit | Set the playlist track limit to pull. | 5000 |
| PullSpotify | --connection-string | ConnectionString for Postgres database. | Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia |
| PullPlex | --spotify-client-id | Spotify Client Id, to use for the Spotify API. | xxxxxxx |
| PullPlex | --spotify-secret-id | Spotify Secret Id, to use for the Spotify API. | xxxxxxx |
| PullPlex | --authentication-redirect-uri | The redirect uri to use for Authentication. | https://xxxxxxxxxxxx.ngrok-free.app/callback |
| PullPlex | --authentication-callback-listener | The callback listener url to use for Authentication. | http://*:5000/callback/ |
| PullPlex | --owner-name | The name of the owner who'se account this belongs to. | user_1234 |
| PullPlex | --liked-playlist-name | Save the liked songs into a specific playlist name, in Spotify liked songs are in 'Liked Songs' which is a fake playlist. | Liked Songs |
| PullSubsonic | --connection-string | ConnectionString for Postgres database. | Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia |
| PullSubsonic | --url | SubSonic server url. | http://xxxxxxx/ |
| PullSubsonic | --username | SubSonic username for authentication. | xxxxxxx |
| PullSubsonic | --password | SubSonic password for authentication. | xxxxxxx |
| PullSubsonic | --liked-playlist-name | Save the liked songs into a specific playlist name, in SubSonic liked songs are not in a playlist. | Liked Songs |
| PullTidal | --connection-string | ConnectionString for Postgres database. | Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia |
| PullTidal | --client-id | Tidal Client Id, to use for the Tidal API. | xxxxxx |
| PullTidal | --secret-id | Tidal Secret Id, to use for the Tidal API. | xxxxxx |
| PullTidal | --authentication-redirect-uri | The redirect uri to use for Authentication. | https://xxxxxx.ngrok-free.app/callback |
| PullTidal | --country-code | Tidal's CountryCode (e.g. US, FR, NL, DE etc). | US |
| PullTidal | --owner-name | The name of the owner who'se account this belongs to. | user_12345 |
| PullTidal | --authentication-callback-listener | The callback listener url to use for Authentication. | http://*:5000/callback/ |
| PullTidal | --liked-playlist-name | Save the liked songs into a specific playlist name, in Tidal liked songs are not in a playlist. | Liked Songs |
| PullJellyfin | --connection-string | ConnectionString for Postgres database. | Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia |
| PullJellyfin | --url | Jellyfin server url. | http://xxxxxxx/ |
| PullJellyfin | --username | Jellyfin username for authentication. | xxxxxxx |
| PullJellyfin | --password | Jellyfin password for authentication. | xxxxxxx |
| PullJellyfin | --favorite-playlist-name | Save the favorite songs into a specific playlist name, in Jellyfin liked songs are not in a playlist. | Favorites |
| Sync | --connection-string | ConnectionString for Postgres database. | Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia |
| Sync | --from-service | Sync from the selected service. | plex |
| Sync | --from-name | Sync from either the name (username etc) or url. | http://xxxxxxx/ |
| Sync | --from-playlist-name | Sync from this specific playlist name. | Liked Some Songs |
| Sync | --from-plex-token | Plex token for authentication. | xxxxxxx |
| Sync | --from-subsonic-username | SubSonic username for authentication. | xxxxxxx |
| Sync | --from-subsonic-password | SubSonic password for authentication. | xxxxxxx |
| Sync | --from-jellyfin-username | Jellyfin username for authentication. | xxxxxxx |
| Sync | --from-jellyfin-password | Jellyfin password for authentication. | xxxxxxx |
| Sync | --from-skip-playlists | Skip to sync by playlist names. | Disliked Songs |
| Sync | --from-skip-prefix-playlists | Skip to sync by playlists that start with prefix(es). | # |
| Sync | --from-skip-prefix-playlists | Skip to sync by playlists that start with prefix(es). | # |
| Sync | --from-tidal-country-code | Tidal's CountryCode (e.g. US, FR, NL, DE etc). | US |
| Sync | --to-service | Sync to the selected service. | spotify |
| Sync | --to-name | Sync to either the name or url. | user_1234 |
| Sync | --to-playlist-name | Sync to this specific playlist name. | Some Playlist |
| Sync | --to-playlist-prefix | Sync to this specific playlist name. | # |
| Sync | --to-plex-token | Plex token for authentication. | xxxxxxx |
| Sync | --to-subsonic-username | SubSonic username for authentication. | xxxxxxx |
| Sync | --to-subsonic-password | SubSonic password for authentication. | xxxxxxx |
| Sync | --to-jellyfin-username | Jellyfin username for authentication. | xxxxxxx |
| Sync | --to-jellyfin-password | Jellyfin password for authentication. | xxxxxxx |
| Sync | --to-tidal-country-code | Tidal's CountryCode (e.g. US, FR, NL, DE etc). | US |
| Sync | --match-percentage | The required amount of % to match a playlist track. | 90 |
| Sync | --like-playlist-name | The name of the like/favorite songs playlist, when using this setting it will like/favorite tracks instead of adding them to a target playlist. | Liked Songs |
| Sync | --force-add-track | Ignore thinking a song was already added to the playlist and try again anyway, useful for recovering backups. | true |
| Sync | --deep-search | If the desired track cannot be found with the normal search method, we'll do a automated search ourself going through the artists, albums to find it. | true |

# Pull Spotify playlists
Personally I would say, since the first authentication with Spotify requires now a HTTPS connection, create a account at https://ngrok.com

After the first authentication, the brower is no longer required, default listening port for localhost callback is 5000

```
dotnet MiniMediaPlaylists.dll pullspotify
--owner-name xxxxxxxxxxxx \
--spotify-client-id xxxxxxxxxxxx \
--spotify-secret-id xxxxxxxxxxxx \
--authentication-redirect-uri "https://xxxxxxxxxxxx.ngrok-free.app/callback"
```

# Pull SubSonic/Navidrome playlists

```
dotnet MiniMediaPlaylists.dll pullsubsonic \
--url "https://navidrome.arr.bottlepost.me \
--username xxxxxxxxxxxx \
--password xxxxxxxxxxxx
```

# Pull Plex playlists

```
dotnet MiniMediaPlaylists.dll pullplex \
--url "http://xxxxxxxxxxxx" \
--token xxxxxxxxxxxx
```

# Pull Jellyfin playlists

```
dotnet MiniMediaPlaylists.dll pulljellyfin \
--url "http://xxxxxxxxxxxx" \
--username xxxxxxxxxxxx \
--password xxxxxxxxxxxx \
--favorite-playlist-name Favorites
```

# Pull Tidal playlists

This example expects you to have ngrok setup, use the command "ngrok http 5000" for the default listener at port 5000

Just like the spotify example

```
dotnet MiniMediaPlaylists.dll pulltidal \
--url "http://xxxxxxxxxxxx" \
--client-id xxxxxxxxxxxx \
--secret-id xxxxxxxxxxxx \
--country-code US \
--owner-name user_12345 \
--authentication-redirect-uri https://xxxxxx.ngrok-free.app/callback
```

# Sync Plex To Navidrome
```
dotnet MiniMediaPlaylists.dll sync \
--from-service plex \
--from-name "http://plex.xxxxxxxxxxxx" \
--to-service subsonic \
--to-name "http://xxxxxxxxxxxx" \
--to-subsonic-username xxxxxxxxxxxx \
--to-subsonic-password xxxxxxxxxxxx
```

# Sync Spotify To Navidrome
```
dotnet MiniMediaPlaylists.dll sync \
--from-service spotify \
--from-name "user_xxxxxxx" \
--to-service subsonic \
--to-name "http://xxxxxxxxxxxx" \
--to-subsonic-username xxxxxxxxxxxx \
--to-subsonic-password xxxxxxxxxxxx
```

# Sync Spotify To Plex
```
dotnet MiniMediaPlaylists.dll sync \
--from-service spotify \
--from-name "user_xxxxxxx" \
--to-service subsonic \
--to-name "http://plex.xxxxxxxxxxxx" \
--to-plex-token xxxxxxxxxxxx
```

# Sync Navidrome To Spotify
```
dotnet MiniMediaPlaylists.dll sync \
--from-service subsonic \
--from-name "http://xxxxxxxxxxxx" \
--to-service spotify \
--to-name "user_xxxxxxx"
```

# Sync Navidrome To Jellyfin
```
dotnet MiniMediaPlaylists.dll sync \
--from-service subsonic \
--from-name "http://xxxxxxxxxxxx" \
--to-service jellyfin \
--to-name "user_xxxxxxx" \
--to-jellyfin-username xxxxxxxxxxxx \
--to-jellyfin-password xxxxxxxxxxxx
```

# Sync Plex To Tidal
```
dotnet MiniMediaPlaylists.dll sync \
--from-service plex \
--from-name "http://plex.xxxxxxxxxxxx" \
--to-service tidal \
--to-name "user_12345" \
--to-tidal-country-code US
```

# Sync Plex To Plex (like restoring a backup)
Set the --to-name/--from-name the exact same

--force-add-track, optin is the trick to restore the backup, bypassing the database if it was synced to the playlist already

--deep-search, will give you a higher chance on success for finding back the missing/deleted songs, for me it went from ~80% success to 99% to restoring the "backup"

```
dotnet MiniMediaPlaylists.dll sync \
--from-service plex \
--from-name "http://plex.xxxxxxxxxxxx" \
--to-service plex \
--to-name "http://plex.xxxxxxxxxxxx" \
--to-plex-token xxxxxxxxxxxx \
--like-playlist-name "❤️ Tracks" \
--force-add-track \
--deep-search
```


