namespace Aria.Core.Library;

public record TrackArtistInfo
{
    public required ArtistInfo Artist { get; init; }

    /// <summary>
    ///     The roles of the artist, as for this track.
    /// </summary>
    /// <example>
    ///     Søren Bebe is both a composer and a performer, as he often performs his own compositions. However, on this
    ///     recording, his music is performed by Lang Lang. Therefore, for this track, he is credited only as the composer, even
    ///     though, as an artist in general, he is also recognized as a performer.
    /// </example>
    public required ArtistRoles Roles { get; init; } = ArtistRoles.None;
}