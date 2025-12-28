using Aria.Infrastructure;
using Aria.MusicServers.MPD;

namespace Aria.App.Infrastructure;

/// <summary>
/// A development connection provider. I plan to develop a connection provider based upon user-configured profiles.
/// </summary>
public class ConnectionProfileProvider : IConnectionProfileProvider
{
    public Task<IEnumerable<IConnectionProfile>> GetAllProfilesAsync()
    {
        return Task.FromResult(CreateProfiles());
    }

    private static IEnumerable<IConnectionProfile> CreateProfiles()
    {
        return new List<IConnectionProfile>
        {
            // Just a dummy MPD connection to my development MPD instance
            new ConnectionProfile
            {
                Host = "192.168.10.10",
                Port = 6600
            }
        };
    }
}