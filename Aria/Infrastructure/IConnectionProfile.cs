namespace Aria.Infrastructure;

/// <summary>
///     Represents a configuration profile for connecting to an integration. Stores the settings and credentials.
/// </summary>
public interface IConnectionProfile
{
    /// <summary>
    /// A user-configurable display name to identify this profile.
    /// </summary>
    /// <example>My home server</example>
    /// <example>MPD (Beach House)</example>
    /// <example>Bedroom</example>
    public string Name { get; set; }
}