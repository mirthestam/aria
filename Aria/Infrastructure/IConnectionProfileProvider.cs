namespace Aria.Infrastructure;

public interface IConnectionProfileProvider
{
    /// <summary>
    /// Gets all the available connection profiles
    /// </summary>
    Task<IEnumerable<IConnectionProfile>> GetAllProfilesAsync();
}