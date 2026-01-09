using MpcNET;

namespace Aria.MusicServers.MPD.Events;

public class StatusChangedEventArgs(MpdStatus status) : EventArgs
{
    public MpdStatus Status { get; init; } = status;
}