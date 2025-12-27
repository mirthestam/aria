namespace Aria.Integrations.MPD.Events;

public class MPDIdleResponseEventArgs(string message) : EventArgs
{
    public string Message { get; init; } = message;
}