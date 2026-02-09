using Aria.Core.Extraction;

namespace Aria.Features.Player.Queue;

public class EnqueueRequestedEventArgs(Id id, int index) : EventArgs
{
    public Id Id { get; } = id;
    public int Index { get; } = index;
}