using Aria.Infrastructure.Connection;

namespace Aria.Backends.MPD.Connection;

public class BackendConnectionFactory(IServiceProvider serviceProvider) : BaseBackendConnectionFactory<BackendConnection, ConnectionProfile>(serviceProvider)
{
    protected override Task ConfigureAsync(BackendConnection connection, ConnectionProfile profile)
    {
        var credentials = new Credentials(profile.Host, profile.Port, profile.Password);
        connection.SetCredentials(credentials);
        return Task.CompletedTask;
    }
}