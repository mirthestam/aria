using Aria.Core.Extraction;

namespace Aria.Core.Library;

/// <summary>
///     Represents an artist in the library.
/// </summary>
//public sealed record ArtistInfo(string Name, string NameSort, ArtistRoles Roles)
public sealed record ArtistInfo
{
    public Id? Id { get; init; }

    public required string Name { get; init; }
    
    /// <summary>
    ///     The roles of the artist in the library.
    /// </summary>
    /// <example>
    ///     This library contains music composed by Søren Bebe. However, it also includes songs in which he performs music
    ///     composed by Chopin. Therefore, as an artist, he is both a performer and a composer.
    /// </example>
    public ArtistRoles Roles { get; init; }
    
}