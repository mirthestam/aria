using Aria.Infrastructure;
using Aria.MusicServers.MPD;
using Zeroconf;

namespace Aria.App.Infrastructure;

/// <summary>
/// A development connection provider. I plan to develop a connection provider based upon user-configured profiles.
/// </summary>
public class ConnectionProfileProvider : IConnectionProfileProvider
{
    private readonly List<IConnectionProfile> _connectionProfiles = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public event EventHandler? DiscoveryCompleted;

    public async Task<IEnumerable<IConnectionProfile>> GetAllProfilesAsync()
    {
        if (_connectionProfiles.Count != 0) return _connectionProfiles;
        
        // Initialize with default profiles
        _connectionProfiles.Add(new ConnectionProfile
        {
            Id = Guid.NewGuid(),
            Name = "This computer",
            Host = "127.0.0.1",
            Port = 6600,
            AutoConnect = false
        });

        // Invoke initial discovery automatically
        _ = UpdateAsync();
        
        return _connectionProfiles;
    }

    public Task<IConnectionProfile?> GetDefaultProfileAsync()
    {
        // No default support yet, as I only have implemented zeroconf discovery and no user configurable profiles
        return Task.FromResult<IConnectionProfile?>(null);
    }

    public async Task UpdateAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await DiscoverServersAsync();
        }
        finally
        {
            _lock.Release();
        }
        
        DiscoveryCompleted?.Invoke(this, EventArgs.Empty);
    }
    
    private async Task DiscoverServersAsync()
    {
        var discoveredProfiles = new List<IConnectionProfile>();
        ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync();            
    
        var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));            
        foreach (var resp in responses)
        {
            var mpdService = resp.Services.FirstOrDefault(s => s.Key.Contains("_mpd."));
            if (mpdService.Value == null) continue;
            
            var profile = new ConnectionProfile
            {
                Id = Guid.NewGuid(),
                AutoConnect = false,
                Host = resp.IPAddress,
                Name = resp.DisplayName,
                Port = mpdService.Value.Port,
                Flags = ConnectionFlags.Discovered 
            };
            discoveredProfiles.Add(profile);
        }

        // Clear earlier discovered entries to avoid duplicates
        _connectionProfiles.RemoveAll(p => p.Flags.HasFlag(ConnectionFlags.Discovered));
        
        // Add the fresh batch
        _connectionProfiles.AddRange(discoveredProfiles);       
    }
}