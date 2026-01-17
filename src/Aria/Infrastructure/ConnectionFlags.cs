namespace Aria.Infrastructure;

[Flags]
public enum ConnectionFlags
{
    None = 0,
    Discovered = 1 << 0
}