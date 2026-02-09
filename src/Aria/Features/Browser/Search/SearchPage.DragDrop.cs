using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Search;

public partial class SearchPage
{
    // DragDrop
    private readonly List<DragSource> _trackDragSources = [];
    private readonly List<DragSource> _albumDragSources = [];
    
    private void ClearDragDrop()
    {
        foreach (var dragSource in _albumDragSources)
        {
            dragSource.OnDragBegin -= AlbumOnOnDragBegin;
            dragSource.OnPrepare -= AlbumDragOnPrepare;            
        }
        _albumDragSources.Clear();
        
        foreach (var dragSource in _trackDragSources)
        {
            dragSource.OnDragBegin -= AlbumOnOnDragBegin;
            dragSource.OnPrepare -= TrackOnPrepare;            
        }
        _albumDragSources.Clear();
    }    
    
    private static ContentProvider AlbumDragOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (SearchAlbumActionRow)sender.GetWidget()!;
        var wrapper = GId.NewForId(widget.AlbumId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }    
    
    private static ContentProvider TrackOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (SearchTrackActionRow)sender.GetWidget()!;
        var wrapper = GId.NewForId(widget.TrackId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }    
    
    private static void AlbumOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
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