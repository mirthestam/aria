using Aria.Infrastructure;

namespace Aria.Backends.Stub;

public class BackendConnectionFactory(IServiceProvider serviceProvider)
    : BaseBackendConnectionFactory<BackendConnection, ConnectionProfile>(serviceProvider);