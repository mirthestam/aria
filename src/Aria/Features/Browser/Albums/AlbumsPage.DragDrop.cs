using Adw;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Albums;

public partial class AlbumsPage
{
    private readonly List<DragSource> _albumDragSources = [];
    
    private void OnItemFactoryOnOnSetup(SignalListItemFactory factory, SignalListItemFactory.SetupSignalArgs args)
    {
        var item = (ListItem)args.Object;
        var child = AlbumsAlbumListItem.NewWithProperties([]);
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Copy;
        dragSource.OnDragBegin += AlbumOnOnDragBegin;
        dragSource.OnPrepare += AlbumOnPrepare;
        child.AddController(dragSource);
        _albumDragSources.Add(dragSource);
        item.SetChild(child);
    }    
    
    private static void AlbumOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
        var widget = (AlbumsAlbumListItem)sender.GetWidget()!;
        var cover = widget.Model!.CoverTexture;
        if (cover == null) return;

        var coverPicture = Picture.NewForPaintable(cover);
        coverPicture.AddCssClass("cover");
        coverPicture.CanShrink = true;
        coverPicture.ContentFit = ContentFit.ScaleDown;
        coverPicture.AlternativeText = widget.Model.Album.Title;

        var clamp = Clamp.New();
        clamp.MaximumSize = 96;
        clamp.SetChild(coverPicture);

        var dragIcon = DragIcon.GetForDrag(args.Drag);
        dragIcon.SetChild(clamp);
    }
    
    private static ContentProvider AlbumOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (AlbumsAlbumListItem)sender.GetWidget()!;
        var wrapper = GId.NewForId(widget.Model!.Album.Id);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }    
}