using Aria.Core.Extraction;

namespace Aria.Features.Player.Queue;

public class MoveRequestedEventArgs(Id sourceId, int targetIndex) : EventArgs
{
    public Id SourceId { get; } = sourceId;
    public int TargetIndex { get; } = targetIndex;
}