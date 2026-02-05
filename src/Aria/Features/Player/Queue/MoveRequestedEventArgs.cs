using Aria.Core.Extraction;

namespace Aria.Features.Player.Queue;

public class MoveRequestedEventArgs : EventArgs
{
    public Id SourceId { get; }
    public int TargetIndex { get; }

    public MoveRequestedEventArgs(Id sourceId, int targetIndex)
    {
        SourceId = sourceId;
        TargetIndex = targetIndex;
    }
}