namespace Aria.Core.Playlist;

public readonly record struct ShuffleSettings
{
    public required bool Enabled { get; init; }
    public required bool Supported { get; init; }
    
    public static ShuffleSettings Default => new ShuffleSettings
    {
        Enabled = false,
        Supported = false
    };
}