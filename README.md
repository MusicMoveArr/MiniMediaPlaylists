# MiniMedia Playlists
MiniMedia's Playlists - Cross-Platform/Provider playlist synchronization

Synchronize the way you want it easily from Spotify To Navidrome, Plex To Spotify whatever your weird combination shall be

No other tool could do this yet so some one had to make this...

Loving the work I do? buy me a coffee https://buymeacoffee.com/musicmovearr

# Roadmap
- [ ] More streaming providers support like Deezer, Tidal etc
- [ ] PreConfiguration Rules files

# Supported services
1. Spotify
2. Plex
3. SubSonic (includes Navidrome)

# Features
1. Postgres support
2. Store the playlists locally as a "backup"
3. Cross-Sync between the providers e.g. Spotify <-> Plex <-> SubSonic/Navidrome

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
    image: musicmovearr/minimediaplaylists:main
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

# Pull Spotify playlists
Personally I would say, since the first authentication with Spotify requires now a HTTPS connection, create a account at [n](https://ngrok.com)

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

#sync Plex To Navidrome
```
dotnet MiniMediaPlaylists.dll sync \
--from-service "plex \
--from-name "http://plex.xxxxxxxxxxxx" \
--to-service subsonic \
--to-name "http://xxxxxxxxxxxx" \
--to-subsonic-username xxxxxxxxxxxx \
--to-subsonic-password xxxxxxxxxxxx
```

#sync Spotify To Navidrome
```
dotnet MiniMediaPlaylists.dll sync \
--from-service "spotify \
--from-name "user_xxxxxxx" \
--to-service subsonic \
--to-name "http://xxxxxxxxxxxx" \
--to-subsonic-username xxxxxxxxxxxx \
--to-subsonic-password xxxxxxxxxxxx
```

#sync Spotify To Plex
```
dotnet MiniMediaPlaylists.dll sync \
--from-service "spotify \
--from-name "user_xxxxxxx" \
--to-service subsonic \
--to-name "http://plex.xxxxxxxxxxxx" \
--to-plex-token xxxxxxxxxxxx
```

#sync Navidrome To Spotify
```
dotnet MiniMediaPlaylists.dll sync \
--from-service "subsonic \
--from-name "http://xxxxxxxxxxxx" \
--to-service spotify \
--to-name "user_xxxxxxx"
```
