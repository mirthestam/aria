using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aria.MusicServers.MPD;

public class BackendConnectionFactory(IMessenger messenger, ITagParser tagParser, IServiceProvider serviceProvider) : IBackendConnectionFactory
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

        // TODO: A default tag parser is currently injected from the container.
        // The intention, however, is to allow the user to select their preferred tag parser.
        // Refactor this to use a tag parser provider that resolves the parser based on the user's profile.

        var connectionLogger = serviceProvider.GetRequiredService<ILogger<BackendConnection>>();
        var libraryLogger = serviceProvider.GetRequiredService<ILogger<Library>>();
        var integration = new BackendConnection(messenger, tagParser, connectionLogger, libraryLogger);
        integration.SetCredentials(credentials);
        return Task.FromResult<IBackendConnection>(integration);
    }
}