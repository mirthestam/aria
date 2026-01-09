namespace Aria.Infrastructure;

public interface IConnectionProfileProvider
{
    /// <summary>
    /// Gets all the available connection profiles
    /// </summary>
    Task<IEnumerable<IConnectionProfile>> GetAllProfilesAsync();
    
    /// <summary>
    /// Gets the connection that the user configured as default.
    /// </summary>
    Task<IConnectionProfile?> GetDefaultProfileAsync();
    
    /// <summary>
    /// Occurs when a discovery has occured and the profile list has been updated.
    /// </summary>
    event EventHandler DiscoveryCompleted;    
}