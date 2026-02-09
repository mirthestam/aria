using Aria.Core.Extraction;

namespace Aria.Core.Library;

public abstract record Info
{
    public required Id Id { get; init; }    
}
