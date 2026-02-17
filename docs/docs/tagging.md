# Tagging

Aria is not a tool for editing music metadata. There are excellent open-source tools available in the community for that
purpose. A good example is the application [Ear Tag](https://flathub.org/en/apps/app.drey.EarTag) , which provides easy-to-use features for managing and editing music metadata.

Another great alternative although slighty complexer is the app [Picard](https://picard-docs.musicbrainz.org/en/index.html) by Musicbrainz.

## Tagging your music
The reality is that the navigability and presentation of your music library depend on the quality of the available
metadata (tags).

Over the years, there have been many different approaches to how metadata should be stored within music files. To
accommodate all these approaches, Aria would need to have an extensive configuration for how it reads metadata. Instead,
I’ve chosen to follow best practices as outlined in the documentation of the server chosen by the user. This ensures
that the user can easily switch between exploring their library in Aria and using other software that is compatible with
the underlying server.

In addition, Aria support the tagging scheme as specified by Picard.

### Picard

> This section is under development and just contains some notes from during development.
> I intend to provide a configuration for picard with minimal deviations from the defaults.

These are instructions how to configure picard for the best result. First of all, we use the default settings of picard as base.

By default, the Album Artist tag from picard is 'As Credited' and is not standardized, and therefore
not usable for artist information. Therefore' we enable the 'MEtadata/ Use standardized artist names'.
Example: 'Beethoven' would become 'Ludwig van Beethoven'.

Also 'Translate artist names to local' will make sure all artists are translated to the selected language. This will implicitly
prevent duplicate artist in different languages.

We use the 'Classical Extras' plugin with default settings to get classical information we can parse.

**Artists and sort tags**

For the sidebar artists, we rely on MPD LIST commands. MPD does not support sorted variants for all exposed tags; for example, conductors cannot be sorted by last name. The only alternative would be to parse the entire database and maintain our own metadata cache.

MPD docs state that the 'artist' tag is vague, and that there are specialized tags like composer and performer.
The way Aria treats this is that ALL artists are 'artists' and they just have roles. So we use the composer, performer tags
etc to identify roles.

https://community.metabrainz.org/t/multiple-album-artists/532302/13
Picard has no default 'albumartists' multi-list. Also, for 'albumartist'  join phrases do not seem to be standardized.

This script, as proposed in the topic above, converts it to a multi list. MPD supports this multi list. Therefore,
we now can handle them.

```
$setmulti(albumartist,%_albumartists%)
$setmulti(albumartistsort,%_albumartists_sort%)
```

If you want to be able to see ensembles in the artists list,
you'll need to use the classical tool to map 'ensemble_names' field to 'ensemble'.
Do note, this is a single field.

> TODO see if we can use this tool properly to improve artists information
https://github.com/rdswift/picard-plugins/blob/2.0_RDS_Plugins/plugins/additional_artists_variables/docs/README.md
