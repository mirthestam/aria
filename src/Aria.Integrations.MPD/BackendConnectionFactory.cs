using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aria.MusicServers.MPD;

public class BackendConnectionFactory(IServiceProvider serviceProvider) : IBackendConnectionFactory
{
    public bool CanHandle(IConnectionProfile profile)
    {
        return profile is ConnectionProfile;
    }

    public Task<IBackendConnection> CreateAsync(IConnectionProfile profile)
    {
        if (profile is not ConnectionProfile mpdProfile)
            throw new ArgumentException("Profile is not an MPD profile");

        var credentials = new Credentials(mpdProfile.Host, mpdProfile.Port, mpdProfile.Password);
        
        var integration = serviceProvider.GetRequiredService<BackendConnection>();
        integration.SetCredentials(credentials);
        return Task.FromResult<IBackendConnection>(integration);
    }
}