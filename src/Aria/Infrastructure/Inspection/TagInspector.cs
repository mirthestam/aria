using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure.Inspection;

public abstract class TagInspector
{
    public abstract IEnumerable<Diagnostic> Inspect(IReadOnlyList<Tag> tags);
}

public abstract class AlbumInspector
{
    public abstract IEnumerable<Diagnostic> Inspect(AlbumInfo album);    
}