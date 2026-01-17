using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Backends.MPD.Extraction;

public class AlbumsParser(ITagParser tagParser)
{
    public IEnumerable<AlbumInfo> GetAlbums(IReadOnlyList<Tag> tags)
    {
        var parsedResults = new List<(AlbumInfo Album, SongInfo Song)>();
        var currentSongTags = new List<Tag>();

        foreach (var tag in tags)
        {
            // If we encounter a new 'file', and we already have data, parse the previous song
            if (tag.Name.Equals("file", StringComparison.OrdinalIgnoreCase) && currentSongTags.Count > 0)
            {
                // This tag indicates a new song.

                // Store our AlbumInfo and SongInfo pair
                parsedResults.Add(ParseAlbumInformation(currentSongTags));
                currentSongTags.Clear();
            }

            currentSongTags.Add(tag);
        }

        // Don't forget the very last song in the stream
        if (currentSongTags.Count > 0)
        {
            parsedResults.Add(ParseAlbumInformation(currentSongTags));
        }

        // 2. Group by Album ID to consolidate the individual songs into a full album
        var uniqueAlbumInfos = parsedResults
            .GroupBy(res => res.Album.Id)
            .Select(group =>
            {
                // The template for the album (metadata like Title, etc.)
                var albumMetadata = group.First().Album;

                // Since each entry in 'group' represents one song found for this album:
                // 1. We extract the Song from each result in the group
                var consolidatedSongs = group
                    .Select(x => x.Song)
                    .ToList();

                // Return the album metadata enriched with the full collection of songs
                return albumMetadata with
                {
                    Songs = consolidatedSongs,
                    CreditsInfo = albumMetadata.CreditsInfo with
                    {
                        // Also aggregate any credits found across all tracks
                        AlbumArtists = group.SelectMany(x => x.Album.CreditsInfo.AlbumArtists).DistinctBy(a => a.Id)
                            .ToList(),
                        Artists = group.SelectMany(x => x.Album.CreditsInfo.Artists).DistinctBy(a => a.Artist.Id)
                            .ToList()
                    }
                };
            });

        // 2. Group by Album ID and consolidate all individual songs into full AlbumInfo objects
        return parsedResults
            .GroupBy(res => res.Album.Id)
            .Select(group =>
            {
                var template = group.First().Album;

                // Collect and deduplicate all songs found for this album
                var consolidatedSongs = group
                    .Select(x => x.Song)
                    .ToList();

                // Merge all credits found across all tracks in this group
                var allAlbumArtists = group.SelectMany(x => x.Album.CreditsInfo.AlbumArtists)
                    .DistinctBy(a => a.Id)
                    .ToList();

                var allArtists = group.SelectMany(x => x.Album.CreditsInfo.Artists)
                    .DistinctBy(a => a.Artist.Id)
                    .ToList();

                // var albumArtId = Id.Empty;
                //
                // // MPD uses the fileName of a song to search for `cover` art files in its containing path.
                // // Therefore, we use a song on this album to query MPD.
                // var artTemplate = uniqueAlbumInfos.LastOrDefault(a => a.Id == template.Id);
                // if (artTemplate is { Songs.Count: > 0 })
                // {
                //     var referenceSong = artTemplate.Songs[0];
                //     if (referenceSong is { Id: SongId id })
                //     {
                //         albumArtId = new AssetId(id);
                //     }
                // }

                var firstSong = consolidatedSongs.First();
                
                return template with
                {
                    Assets = firstSong.Assets, // Take the assets from the first song
                    Songs = consolidatedSongs,
                    CreditsInfo = template.CreditsInfo with
                    {
                        AlbumArtists = allAlbumArtists,
                        Artists = allArtists
                    }
                };
            });
    }

    private (AlbumInfo Album, SongInfo Song) ParseAlbumInformation(List<Tag> songTags)
    {
        var songInfo = tagParser.ParseSongInformation(songTags);

        if (songInfo.FileName != null)
        {
            // For MPD we want to look up album art by filename
            songInfo = songInfo with
            {
                Assets =
                [
                    new AssetInfo
                    {
                        Id = new AssetId(songInfo.FileName),
                        Type = AssetType.FrontCover
                    }
                ]
            };
        }


        var albumInfo = tagParser.ParseAlbumInformation(songTags);

        return (albumInfo, songInfo);
    }
}