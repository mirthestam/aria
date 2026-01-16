namespace Aria.Core.Playlist;

public readonly record struct RepeatSettings
{
    public required bool Enabled { get; init; } 
    public required bool Single { get; init; } 
    public required bool Supported { get; init; }
    
    public static RepeatSettings Default => new RepeatSettings
    {
        Enabled = false,
        Single = false,
        Supported = false
    };
}