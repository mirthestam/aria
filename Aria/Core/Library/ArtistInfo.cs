namespace Aria.Core.Library;

/// <summary>
///     Represents an artist in the library.
/// </summary>
/// <param name="Name">The name of the artist.</param>
/// <param name="NameSort">The name  of the artist, for sorting purposes.</param>
public sealed record ArtistInfo(Id Id, string Name, string NameSort, ArtistRoles Roles)
{
    /// <summary>
    ///     The roles of the artist in the library.
    /// </summary>
    /// <example>
    ///     This library contains music composed by Søren Bebe. However, it also includes songs in which he performs music
    ///     composed by Chopin. Therefore, as an artist, he is both a performer and a composer.
    /// </example>
    public ArtistRoles Roles { get; init; } = Roles;
}