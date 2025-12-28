namespace Aria.MusicServers.MPD.Events;

public class IdleResponseEventArgs(string message) : EventArgs
{
    public string Message { get; init; } = message;
}