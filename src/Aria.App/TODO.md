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

### IMPROVEMENT: MPD Reconnection

The MPD backend currently has no good recovery when the connection is lost. It should attempt to reconnect
automatically.

### IDEA: Blur

I like the Blur The Euphonica App did. Think if this is a good addition for us, and our target audience.
Amberol also does it.

### DISCUSSION: Sidebar

The sidebar currently only has artists. I wanto to be able to: Show all albums. Switch to composers.
I need to work out the UI for that. Let's use cambalanche demo project for that?