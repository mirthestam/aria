using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Album;

public partial class TrackGroup
{
    private static ContentProvider TrackOnDragPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var row = (AlbumTrackRow)sender.GetWidget()!;
        var wrapper = new GId(row.TrackId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }
    
    private static void InitializeTrackDragSource(AlbumTrackRow row)
    {
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Copy;
        dragSource.OnPrepare += TrackOnDragPrepare;
        row.AddController(dragSource);
    }    
}