using Aria.Core;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Player.Playlist;

[Subclass<Object>]
public partial class SongModel
{
    public SongModel(Id id,
        string title,
        string subTitle,
        string composerLine,
        TimeSpan duration) : this()
    {
        Id = id;
        Title = title;
        Subtitle = subTitle;
        ComposerLine = composerLine;
        Duration = duration;
    }

    public Id Id { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string ComposerLine { get; set; }
    public TimeSpan Duration { get; set; }
}