using MpcNET;

namespace Aria.Backends.MPD.Connection;

public class StatusChangedEventArgs(MpdStatus status) : EventArgs
{
    public MpdStatus Status { get; } = status;
}