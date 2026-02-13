using Aria.Core.Extraction;

namespace Aria.Features.Player.Queue;

public class EnqueueRequestedEventArgs(Id id, uint index) : EventArgs
{
    public Id Id { get; } = id;
    public uint Index { get; } = index;
}