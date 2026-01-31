# Notes


## Instructions

### Picard

MPD works best with the Picard scheme.
These are instructions how to configure picard for the best result.
First of all, we use the default settings of picard as base.

By default, the Album Artist tag from picard is 'As Credited' and is not standardized, and therefore
not usable for artist information. Therefore' we enable the 'MEtadata/ Use standardized artist names'.
Example: 'Beethoven' would become 'Ludwig van Beethoven'.

Also 'Translate artist names to local' will make sure all artists are translated to the selected language. This will implicitly
prevent duplicate artist in different languages.

We use the 'Classical Extras' plugin with default settings to get classical information we can parse.

**Artists and sort tags**

For the sidebar artists, we rely on MPD LIST commands. MPD does not support sorted variants for all exposed tags; for example, conductors cannot be sorted by last name. The only alternative would be to parse the entire database and maintain our own metadata cache.

For normal queries like albums and tracks, I do not retrieve the 'sort' information.
If I would, I have to write parsing logic for the 'sort' tags, however except for in the sidebar we don't use it.

MPD docs state that the 'artist' tag is vague, and that there are specialized tags like composer and performer.
The way Aria treats this, is that ALL artists are 'artists' and they just have roles. So we use the composer, performer tags
etc to identify roles.
SO, if the artist is found in the artists, and has a sort, even for unsorted roles, we have sorting information IF they are present in the artists.
for which Aria has hits own rule that specialized artists should also be IN the artists as, in fact, they ARE an artist.