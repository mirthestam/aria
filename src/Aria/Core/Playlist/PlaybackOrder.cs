namespace Aria.Core.Playlist;

public readonly record struct PlaybackOrder
{
    public int? CurrentIndex { get; init; }
    public required bool HasNext { get; init; }
    
    public static PlaybackOrder Default => new PlaybackOrder
    {
        CurrentIndex = null,
        HasNext = false
    };
}