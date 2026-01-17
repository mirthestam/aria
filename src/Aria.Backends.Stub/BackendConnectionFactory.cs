using Aria.Infrastructure;
using Aria.Infrastructure.Connection;

namespace Aria.Backends.Stub;

public class BackendConnectionFactory(IServiceProvider serviceProvider)
    : BaseBackendConnectionFactory<BackendConnection, ConnectionProfile>(serviceProvider);