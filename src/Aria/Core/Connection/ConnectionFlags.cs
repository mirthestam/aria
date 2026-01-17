namespace Aria.Core.Connection;

[Flags]
public enum ConnectionFlags
{
    None = 0,
    Discovered = 1 << 0
}