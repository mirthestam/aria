# Docs
This is a placeholder for text that may someday serve as inspiration for the actual documentation.

- [Developer Docs](#developer-docs)
- [User Docs](#user-docs)

## Developer Docs

### Architecture
I chose MVP over MvvM because there is no good bindings in GTK like we had in WPF.
(There are some kind of bindings and expressions but they are all at unmanaged level)

**View**: the UI definitions (SomePage.ui and SomePage.cs).
.ui defines how the UI looks.
.cs is the code behind. Handles commands from the presenter. Throws events.
Knows the GTK widgets.

**Presenter** This is the application logic. The 'conductor'. It retreives information from services (i.e. the IPlayerApi).
It responds to messages (through IMessenger) and tells the view what to show.
It is NOT aware of specific UI controls (i.e. a GTK Listview). It uses the methods exposed by the view.

**Model**: My records, like the 'ArtistInfo', 'AmbumInfo' and services like 'IPlaybackApi, AppSession'

Even though the UI is MVP, I want the mentality of MvvM. Because of decoupling and reactivity.
Therefore, 'State' is central. And we have messaging.

## User Docs

### Why Aria exists?
While the open-source world has seen its fair share of music clients, Aria stands apart by focusing on the way you
interact with your music library, offering a more immersive and personalized experience.

The GNOME CORE Music app is a pure music player that provides a basic view of local music files, displaying them at the
album and artist level.

Aria does not focus on implementing the player software itself. There are several open-source systems, like MPD and
Lyrion, that prioritize sound quality and are perfect for audiophiles seeking the best audio experience.

Aria functions as a client for these systems, enhancing the user experience by providing a simple, intuitive interface
that makes it easier to explore, organize, and enjoy your music collection. It appeals to users who are passionate about
discovering new music and exploring deep connections between artists, albums, and genres. Additionally, Aria ensures
that classical music enthusiasts are not excluded, with a focus on composers, works, and performing artists.

Unlike many other players that overwhelm you with endless features and complex configurations designed for tech-savvy
users, Aria focuses on providing an enjoyable and accessible experience for the everyday music lover. It’s about helping
you appreciate and immerse yourself in your digital music collection in the most intuitive and pleasant way possible,
without unnecessary complexity.

Aria is not a tool for editing music metadata. There are excellent open-source tools available in the community for that
purpose. A good example is the application [Ear Tag](https://flathub.org/en/apps/app.drey.EarTag)
, which provides easy-to-use features for managing and editing music metadata.

### Meta Data
The reality is that the navigability and presentation of your music library depend on the quality of the available
metadata (tags).

Over the years, there have been many different approaches to how metadata should be stored within music files. To
accommodate all these approaches, Aria would need to have an extensive configuration for how it reads metadata. Instead,
I’ve chosen to follow best practices as outlined in the documentation of the server chosen by the user. This ensures
that the user can easily switch between exploring their library in Aria and using other software that is compatible with
the underlying server.

**TODO** Document the  different tagging profiles that one can choose.