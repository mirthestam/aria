using System.Text;
using Aria.Infrastructure;

namespace Aria.MusicServers.MPD;

public class ConnectionProfile : IConnectionProfile
{
    private const int Defaultport = 6600;
        
    public Guid Id { get; init; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 6600;
    public string Password { get; set; } = string.Empty;
    
    public string Name { get; set; } = "Unnamed MPD Connection";
    public bool AutoConnect { get; set; } = false;
    
    public string ConnectionDisplayString => $"{Host}:{Port}";
    public ConnectionFlags Flags { get; set; } = ConnectionFlags.None;
}