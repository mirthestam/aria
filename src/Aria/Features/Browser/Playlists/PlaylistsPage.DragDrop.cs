using Gdk;
using Gtk;

namespace Aria.Features.Browser.Playlists;

public partial class PlaylistsPage
{
    // Drag Drop
    private ContentProvider? DragSourceOnOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        return null;
    }

    private void DragSourceOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
        
    }
}