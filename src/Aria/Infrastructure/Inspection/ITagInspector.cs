using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure.Inspection;

public interface ITagInspector
{
    public Inspected<AlbumInfo> InspectAlbum(IReadOnlyList<Tag> sourceTags);
}