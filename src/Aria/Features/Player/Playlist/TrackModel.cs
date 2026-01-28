using Aria.Core;
using Aria.Core.Extraction;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Player.Playlist;

[Subclass<Object>]
public partial class TrackModel
{
    public TrackModel(Id trackId,
        Id queueTrackId,
        int position,
        string title,
        string subTitle,
        string composerLine,
        TimeSpan duration) : this()
    {
        TrackId = trackId;
        QueueTrackId = queueTrackId;
        Position = position;
        Title = title;
        Subtitle = subTitle;
        ComposerLine = composerLine;
        Duration = duration;
    }
    
    public Id TrackId { get; }
    public Id QueueTrackId { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string ComposerLine { get; }
    public TimeSpan Duration { get; }
    public int Position { get; set; }
}