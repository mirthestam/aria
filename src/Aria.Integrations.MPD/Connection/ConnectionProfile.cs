using Aria.Core.Connection;

namespace Aria.Backends.MPD.Connection;

public class ConnectionProfile : IConnectionProfile
{
    private const int Defaultport = 6600;
        
    public Guid Id { get; init; }
    public string Host { get; set; } = string.Empty;
    public string Socket { get; set; } = "/run/mpd/socket";
    public int Port { get; set; } = 6600;
    public string Password { get; set; } = string.Empty;

    public bool UseSocket { get; set; } = false;
    
    public string Name { get; set; } = "Unnamed MPD Connection";
    public bool AutoConnect { get; set; } = false;
    
    public string ConnectionDisplayString => UseSocket ? $"socket: {Socket}" : $"host: {Host}:{Port}";

    public ConnectionFlags Flags { get; set; } = ConnectionFlags.None;
}