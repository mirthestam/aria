using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Infrastructure.Tagging;
using Microsoft.Extensions.DependencyInjection;

namespace Aria.Infrastructure.Connection;

public class ScopedBackendConnection(IBackendConnection backendConnection, IServiceScope scope) : IDisposable
{
    public IBackendConnection Connection => backendConnection;

    public void Dispose()
    {
        Connection.Dispose();
        scope.Dispose();
    }
}

public class BaseBackendConnectionFactory<TBackendConnection, TConnectionProfile>(IServiceProvider serviceProvider) : IBackendConnectionFactory
    where TBackendConnection : IBackendConnection
    where TConnectionProfile : IConnectionProfile
{
    public virtual bool CanHandle(IConnectionProfile profile)
    {
        return profile is TConnectionProfile;
    }

    public async Task<ScopedBackendConnection> CreateAsync(IConnectionProfile profile)
    {
        if (profile is not TConnectionProfile connectionProfile) throw new ArgumentException("Profile is not an supported profile");
        
        // We use a scope for this factory so all its dependencies are scoped to this connection instance
        var scope = serviceProvider.CreateScope();
        try
        {
            var connection = scope.ServiceProvider.GetRequiredService<TBackendConnection>();
            
            if (connection is BaseBackendConnection baseConnection)
            {
                // The idea here is that in the future, the connectionProfile can influence the selected tag parser.
                baseConnection.SetTagParser( scope.ServiceProvider.GetRequiredService<ITagParser>());
            }          
            
            await ConfigureAsync(connection, connectionProfile);
            
            return new ScopedBackendConnection(connection, scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    protected virtual Task ConfigureAsync(TBackendConnection connection, TConnectionProfile profile)
    {
        return Task.CompletedTask; 
    }
}