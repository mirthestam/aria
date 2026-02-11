using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Playlists;

public partial class PlaylistsPage
{
    // Drag Drop
    private ContentProvider? DragSourceOnOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = sender.GetWidget();
        if (widget is not PlaylistNameCell cell) return null;
        
        var wrapper = GId.NewForId(cell.Model.Playlist.Id);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }

    private void DragSourceOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
        // TODO: when album art is available in the album row,
        // Use the code below to use it as the drag icon.
        
        // var widget = (SearchAlbumActionRow)sender.GetWidget()!;
        // var cover = widget.Model!.CoverTexture;
        // if (cover == null) return;
        
        // var coverPicture = Picture.NewForPaintable(cover);
        // coverPicture.AddCssClass("cover");
        // coverPicture.CanShrink = true;
        // coverPicture.ContentFit = ContentFit.ScaleDown;
        // coverPicture.AlternativeText = widget.Model.Album.Title;
        //
        // var clamp = Clamp.New();
        // clamp.MaximumSize = 96;
        // clamp.SetChild(coverPicture);
        //
        // var dragIcon = DragIcon.GetForDrag(args.Drag);
        // dragIcon.SetChild(clamp);        
    }
}