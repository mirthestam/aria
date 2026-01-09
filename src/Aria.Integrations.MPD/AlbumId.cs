using Aria.Core;
using Aria.Infrastructure.Tagging;

namespace Aria.MusicServers.MPD;

/// <summary>
/// MPD Albums are identified by their name and their album artists to avoid collisions between
/// different albums with the same title (e.g., "Greatest Hits").
/// </summary>
public class AlbumId(string identity) : Id.TypedId<string>(identity, "ALB")
{
    // TODO: Add the release date to this album.
    // Note: Artists may have multiple albums with the same name, such as deluxe editions.
    
    public static AlbumId FromContext(AlbumTagParserContext context)
    {
        var title = context.Album.Title;
        
        // Join all album artist names to create a stable part of the identity
        var artists = string.Join("|", context.Album.CreditsInfo.AlbumArtists
            .Select(a => a.Name)
            .OrderBy(n => n));

        // Combine title and artists into a single unique string identity
        var identity = string.IsNullOrWhiteSpace(artists) 
            ? title 
            : $"{title} [{artists}]";

        return new AlbumId(identity);
    }
}