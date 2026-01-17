using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.MusicServers.MPD;

public class BackendConnectionFactory(IServiceProvider serviceProvider) : BaseBackendConnectionFactory<BackendConnection, ConnectionProfile>(serviceProvider)
{
    public override void Configure(BackendConnection connection, ConnectionProfile profile)
    {
        var credentials = new Credentials(profile.Host, profile.Port, profile.Password);
        connection.SetCredentials(credentials);
    }
}