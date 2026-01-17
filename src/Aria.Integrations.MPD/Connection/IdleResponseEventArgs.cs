namespace Aria.Backends.MPD.Connection;

public class IdleResponseEventArgs(string message) : EventArgs
{
    public string Message { get; init; } = message;
}