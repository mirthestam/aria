using Aria.Infrastructure;

namespace Aria.MusicServers.MPD;

public class ConnectionProfile : IConnectionProfile
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 6600;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = "Unnamed MPD Connection";
}