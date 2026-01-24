using Aria.Core.Connection;
using Aria.Features.Shell.Welcome;

namespace Aria.Backends.MPD.Connection;

public class ConnectionProfileFactory : IConnectionProfileFactory
{
    public IConnectionProfile CreateProfile()
    {
        return new ConnectionProfile
        {
            Name = "My computer",
            UseSocket = false,
            AutoConnect = true,
            Host = "127.0.0.1",
            Port = 6600
        };
    }
}