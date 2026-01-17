using Aria.Infrastructure.Tagging;
using Microsoft.Extensions.DependencyInjection;

namespace Aria.Infrastructure;

public class BaseBackendConnectionFactory<TBackendConnection, TConnectionProfile>(IServiceProvider serviceProvider) : IBackendConnectionFactory
    where TBackendConnection : IBackendConnection
    where TConnectionProfile : IConnectionProfile
{
    public virtual bool CanHandle(IConnectionProfile profile)
    {
        return profile is TConnectionProfile;
    }

    public Task<IBackendConnection> CreateAsync(IConnectionProfile profile)
    {
        if (profile is not TConnectionProfile connectionProfile) throw new ArgumentException("Profile is not an supported profile");
        var connection = serviceProvider.GetRequiredService<TBackendConnection>();

        if (connection is BaseBackendConnection baseConnection)
        {
            // The idea here is that in the future, the connectionProfile can influence the selected tag parser.
            baseConnection.SetTagParser( serviceProvider.GetRequiredService<ITagParser>());
        }
        
        Configure(connection, connectionProfile);
        return Task.FromResult<IBackendConnection>(connection);
    }

    public virtual void Configure(TBackendConnection connection, TConnectionProfile profile)
    {
    }
}