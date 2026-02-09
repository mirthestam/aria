namespace Aria.Core.Extraction;

public sealed class QueueTrackBaseIdentificationContext : BaseIdentificationContext
{
    public required IReadOnlyList<Tag> Tags { get; init; }
}