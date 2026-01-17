namespace Aria.Core.Queue;

public readonly record struct ConsumeSettings
{
    public required bool Enabled { get; init; }
    public required bool Supported { get; init; }
    
    public static ConsumeSettings Default => new ConsumeSettings
    {
        Enabled = false,
        Supported = false
    };
}