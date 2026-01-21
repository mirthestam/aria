using Aria.Backends.MPD.Connection;
using Aria.Core.Connection;
using Zeroconf;

namespace Aria.App.Infrastructure;

public class ConnectionProfileProvider(DiskConnectionProfileSource diskSource) : IConnectionProfileProvider
{
    private readonly List<IConnectionProfile> _connectionProfiles = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isLoaded;

    public event EventHandler? DiscoveryCompleted;

    public async Task<IEnumerable<IConnectionProfile>> GetAllProfilesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_isLoaded) return _connectionProfiles.ToList();
            
            var stored = await diskSource.LoadAllAsync();
            _connectionProfiles.AddRange(stored);
            _isLoaded = true;
                
            // Invoke initial discovery automatically
            _ = UpdateAsync();
            return _connectionProfiles.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveProfileAsync(IConnectionProfile profile)
    {
        await _lock.WaitAsync();
        try
        {
            var existing = _connectionProfiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existing != null)
            {
                _connectionProfiles.Remove(existing);
            }
            _connectionProfiles.Add(profile);
            await DiskConnectionProfileSource.SaveAsync(profile);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PersistProfileAsync(Guid id)
    {
        var profile = _connectionProfiles.FirstOrDefault(p => p.Id == id);
        
        if (profile == null) throw new InvalidOperationException("No profile found with the given ID");
        
        profile.Flags &= ~ConnectionFlags.Discovered;
        profile.Flags |= ConnectionFlags.Saved;
        
        await SaveProfileAsync(profile);
    }

    public async Task DeleteProfileAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            _connectionProfiles.RemoveAll(p => p.Id == id);
            DiskConnectionProfileSource.Delete(id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<IConnectionProfile?> GetDefaultProfileAsync()
    {
        // No default support yet, as I only have implemented zeroconf discovery and no user configurable profiles
        // TODO: this should be the first STORED profile that has AutoConnect enabled
        // and later, we want to remember the last used so we auto connect to the last used.
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