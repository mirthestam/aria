using Aria.Core.Extraction;
using Aria.Infrastructure.Extraction;

namespace Aria.Backends.MPD.Extraction;

public class QueueTrackId(int id) : Id.TypedId<int>(id, Key)
{
    public const string Key = "QUE";
    
    public static Id Parse(int value)
    {
        return new QueueTrackId(value);
    }

    public static Id FromContext(QueueTrackIdentificationContext context)
    {
        var idString = context.Tags.First(t => t.Name.Equals(PicardTagNames.QueueTags.Id, StringComparison.InvariantCultureIgnoreCase)).Value;
        var id = int.Parse(idString);
        return new QueueTrackId(id);
    }
}