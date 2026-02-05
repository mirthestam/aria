using Aria.Core.Extraction;

namespace Aria.Features.Player.Queue;

public class EnqueueRequestedEventArgs : EventArgs
{
    public Id Id { get; }
    public int Index { get; }

    public EnqueueRequestedEventArgs(Id id, int index)
    {
        Id = id;
        Index = index;
    }
}