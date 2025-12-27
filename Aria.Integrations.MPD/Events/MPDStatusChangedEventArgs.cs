using MpcNET;

namespace Aria.Integrations.MPD.Events;

public class MPDStatusChangedEventArgs(MpdStatus status) : EventArgs
{
    public MpdStatus Status { get; init; } = status;
}