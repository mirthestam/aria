
DRAG&DROP
- Drag source; alcum cover on album page
- Drag sources; all rows on the search results

BUGS
- convert pill to dropdown, with (refresh) option to manually trigger.
- VariousArtists don't appear in the album list  (becuse there is no Album Artist).
- AlbumCover placeholder is visible on album page, when not loaded yet.

WISHES
- General: Translations

- Sidebar: Implement Sidebar switch (composers, artists)
- Sidebar: Update database trigger

- AlbumPage: Fix albumpage header
- AlbumPage: Drag drop song
- AlbumPage: Track context-menu

- AlbumsPage: Drag drop album
- AlbumsPage: Album Context-menu

- Player: Menu with repeat/random/volume controls
 
- Playlist: Drag drop to move

### FEATURE: Embedded MPD Server Capability

- Refer to Cantata to see how they implement this.

### FEATURE: Automatically Set Embedded MPD as Default

- If no local servers are found, automatically start an embedded MPD server using `XDG-DATA-MUSIC`.
- The server should keep running as long as the profile exists.
- Deleting the profile should stop the embedded server.

### FEATURE: MPRIS Support

### IMPROVEMENT: Coloring of center window

The current grid views use the navigation CSS class. However, when compared to Files (Nautilus), that class is not
applied there. I want the UI to match Nautilus, specifically using its header and grid color scheme, rather than the
styling we implemented here.

### IDEA: Blur

I like the Blur The Euphonica App did. Think if this is a good addition for us, and our target audience.
Amberol also does it.

## Sticker support
https://github.com/jcorporation/mpd-stickers


# Art needed
- Default cover art
- Application Logo
- Playlist button
- Composer Icon symbpolic
- Performer Icon symbolic (violin?)
- Conductor icon symbolic
- Album Icon symbolic
- Artist (general) icon symbolic
- Library icon (normal, symbolic)