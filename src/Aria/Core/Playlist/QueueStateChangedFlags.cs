namespace Aria.Core.Playlist;

[Flags]
public enum QueueStateChangedFlags
{
    None = 0,
    Id = 1 << 0,
    Queue = 1 << 1, 
    PlaybackOrder = 1 << 2,
    Shuffle = 1 << 3,
    Repeat = 1 << 4,
    Consume = 1 << 5,
    All = ~0
}