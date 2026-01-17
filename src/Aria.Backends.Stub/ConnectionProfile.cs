using Aria.Infrastructure;

namespace Aria.Backends.Stub;


public class ConnectionProfile : IConnectionProfile
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Stub connection";
    public bool AutoConnect { get; set; } = false;
    public string ConnectionDisplayString { get; } = "stub:42";
    public ConnectionFlags Flags { get; set; }
}