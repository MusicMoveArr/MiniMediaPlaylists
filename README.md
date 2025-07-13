# MiniMedia Playlists
MiniMedia's Playlists - Cross-Platform/Provider playlist synchronization

Synchronize the way you want it easily from Spotify To Navidrome, Plex To Spotify whatever your weird combination shall be

No other tool could do this yet so some one had to make this...

Loving the work I do? buy me a coffee https://buymeacoffee.com/musicmovearr

# Roadmap
- [ ] More streaming providers support like Deezer, Tidal etc
- [ ] PreConfiguration Rules files
- [ ] Playlist versioning (Snapshots)

# Supported services
1. Spotify
2. Plex
3. SubSonic (includes Navidrome)

# Features
1. Postgres support
2. Store the playlists locally as a "backup"
3. Cross-Sync between the providers e.g. Spotify <-> Plex <-> SubSonic/Navidrome
4. Cross-Sync the liked songs + the song ratings

# Commands
1. PullPlex - Pull all your Plex playlists
2. PullSpotify - Pull all your spotify playlists
3. PullSubsonic - Pull all your SubSonic playlists
4. Sync - Sync playlists between 2 services

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
| Sync | --connection-string | ConnectionString for Postgres database. | Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia |
| Sync | --from-service | Sync from the selected service. | plex |
| Sync | --from-name | Sync from either the name (username etc) or url. | http://xxxxxxx/ |
| Sync | --from-playlist-name | Sync from this specific playlist name. | Liked Some Songs |
| Sync | --from-plex-token | Plex token for authentication. | xxxxxxx |
| Sync | --from-subsonic-username | SubSonic username for authentication. | xxxxxxx |
| Sync | --from-subsonic-password | SubSonic password for authentication. | xxxxxxx |
| Sync | --from-skip-playlists | Skip to sync by playlist names. | Disliked Songs |
| Sync | --from-skip-prefix-playlists | Skip to sync by playlists that start with prefix(es). | # |
| Sync | --to-service | Sync to the selected service. | spotify |
| Sync | --to-name | Sync to either the name or url. | user_1234 |
| Sync | --to-playlist-name | Sync to this specific playlist name. | Some Playlist |
| Sync | --to-playlist-prefix | Sync to this specific playlist name. | # |
| Sync | --to-plex-token | Plex token for authentication. | xxxxxxx |
| Sync | --to-subsonic-username | SubSonic username for authentication. | xxxxxxx |
| Sync | --to-subsonic-password | SubSonic password for authentication. | xxxxxxx |
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

# sync Plex To Navidrome
```
dotnet MiniMediaPlaylists.dll sync \
--from-service "plex \
--from-name "http://plex.xxxxxxxxxxxx" \
--to-service subsonic \
--to-name "http://xxxxxxxxxxxx" \
--to-subsonic-username xxxxxxxxxxxx \
--to-subsonic-password xxxxxxxxxxxx
```

# sync Spotify To Navidrome
```
dotnet MiniMediaPlaylists.dll sync \
--from-service "spotify \
--from-name "user_xxxxxxx" \
--to-service subsonic \
--to-name "http://xxxxxxxxxxxx" \
--to-subsonic-username xxxxxxxxxxxx \
--to-subsonic-password xxxxxxxxxxxx
```

# sync Spotify To Plex
```
dotnet MiniMediaPlaylists.dll sync \
--from-service "spotify \
--from-name "user_xxxxxxx" \
--to-service subsonic \
--to-name "http://plex.xxxxxxxxxxxx" \
--to-plex-token xxxxxxxxxxxx
```

# sync Navidrome To Spotify
```
dotnet MiniMediaPlaylists.dll sync \
--from-service "subsonic \
--from-name "http://xxxxxxxxxxxx" \
--to-service spotify \
--to-name "user_xxxxxxx"
```


