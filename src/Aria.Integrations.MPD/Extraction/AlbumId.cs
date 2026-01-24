using System.Text;
using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

/// <summary>
/// MPD Albums are identified by their title + album artist IDs to avoid collisions between
/// different albums with the same title (e.g., "Greatest Hits").
/// </summary>
public class AlbumId : Id.TypedId<string>
{
    public const string Key = "ALB";
    
    public string Title { get; }
    public IReadOnlyList<ArtistId> AlbumArtistIds { get; }

    // Safe separators (identity uses Base64Url so these won't occur inside parts)
    private const char PartSeparator = '\u001F'; // Unit Separator
    private const char ListSeparator = '\u001E'; // Record Separator

    private AlbumId(string identity, string title, IReadOnlyList<ArtistId> albumArtistIds)
        : base(identity, Key)
    {
        Title = title;
        AlbumArtistIds = albumArtistIds;
    }

    public static AlbumId FromContext(AlbumIdentificationContext context)
    {
        var title = (context.Album.Title ?? string.Empty).Trim();

        // Prefer IDs over names; names can change.
        // Note: If MPD sometimes can't provide IDs here, you need a fallback strategy elsewhere.
        var artistIds = context.Album.CreditsInfo.AlbumArtists
            .Select(a => a.Id)
            .OfType<ArtistId>()
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();

        return FromParts(title, artistIds);
    }

    public static AlbumId FromParts(string title, IEnumerable<ArtistId> albumArtistIds)
    {
        title = (title ?? string.Empty).Trim();

        var artists = (albumArtistIds ?? Array.Empty<ArtistId>())
            .Where(a => a is not null)
            .OrderBy(a => a.ToString(), StringComparer.Ordinal)
            .ToArray();

        var identity = BuildIdentity(title, artists);
        return new AlbumId(identity, title, artists);
    }

    /// <summary>
    /// Used for deserialization, including drag-and-drop scenarios.
    /// </summary>
    public static AlbumId ParseIdentity(string identity, Func<string, ArtistId> parseArtistId)
    {
        if (string.IsNullOrWhiteSpace(identity))
            return new AlbumId(string.Empty, string.Empty, Array.Empty<ArtistId>());

        var parts = identity.Split(PartSeparator);
        var title = parts.Length >= 1 ? Decode(parts[0]) : string.Empty;

        var artists = Array.Empty<ArtistId>();
        if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
        {
            var encodedArtistIdStrings = parts[1].Split(ListSeparator);
            artists = encodedArtistIdStrings
                .Select(Decode)                 // decode back to artist-id-string
                .Select(parseArtistId)          // convert string -> ArtistId object
                .OrderBy(a => a.ToString(), StringComparer.Ordinal)
                .ToArray();
        }

        return new AlbumId(identity, title, artists);
    }

    private static string BuildIdentity(string title, IReadOnlyList<ArtistId> artists)
    {
        var encodedTitle = Encode(title);

        if (artists.Count == 0)
            return encodedTitle;

        // Store artist IDs as strings (whatever ArtistId.ToString() returns in your system),
        // then Base64Url encode them so separators are safe.
        var encodedArtists = string.Join(
            ListSeparator,
            artists.Select(a => Encode(a.ToString())));

        return $"{encodedTitle}{PartSeparator}{encodedArtists}";
    }

    // Base64Url encoding
    private static string Encode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string Decode(string value)
    {
        value ??= string.Empty;
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }

        var bytes = Convert.FromBase64String(s);
        return Encoding.UTF8.GetString(bytes);
    }
}