BUGS
- When welcome page is refreshng, the shown GUID are invalid.
- convert pill to dropdown, with (refresh) option to manually trigger.
- Tagging: When selecting an album for an artist, it only shows tracks including that artist.
- example: 

WISHES
- General: Implement connection profile storage
- General: Translations

- Shell:  miniplayer progressbar visibility
- Shell: mobile view navigation
- Shell; select first artist instead of all albums; on connect

- Sidebar: Implement Sidebar switch (composers, artists)
- Sidebar: Tooltip for items
- Sidebar: Update database trigger

- AlbumPage: Fix albumpage header
- AlbumPage: Album page track selection enqueue
- AlbumPage: Drag drop song
- AlbumPage: Track context-menu

- AlbumsPage: Drag drop album
- AlbumsPage: Album Context-menu

- Player: Menu with repeat/random/volume controls
- Player: Progress bar control and tooltip
 
- Playlist: Drag drop to move

- Search: Everything

### FEATURE: Embedded MPD Server Capability

- Refer to Cantata to see how they implement this.

### FEATURE: Server Detection

- Check if a connection to servers is possible (via AVAHI or local connections).
- Aligns with HIG guidelines: more automatic behavior.

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