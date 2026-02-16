using System.ComponentModel;
using Adw;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Shared;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Shared.AlbumListItem.ui")]
public partial class AlbumListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    
    [Connect("gesture-click")] GestureClick _gestureClick;
    [Connect("gesture-long-press")] GestureLongPress _gestureLongPress;
    [Connect("drag-source")] private DragSource _dragSource;
    
    public GestureClick GestureClick => _gestureClick;
    public GestureLongPress GestureLongPress => _gestureLongPress;
    
    public AlbumModel? Model { get; private set; }

    partial void Initialize()
    {
        _dragSource.OnDragBegin += DragOnOnDragBegin;
        _dragSource.OnPrepare += DragOnPrepare;
    }

    public void Bind(AlbumModel model)
    {
        if (Model != null)
        {
            _coverPicture.SetPaintable(null);            
            Model.PropertyChanged -= ModelOnPropertyChanged;
        }

        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;

        // TODO: I can sort here now with role on priority
        var artistsLine = string.Join(", ", model.Album.CreditsInfo.AlbumArtists.Select(a => a.Artist.Name));

        _titleLabel.SetLabel(model.Album.Title);
        _subTitleLabel.SetLabel(artistsLine);
        
        UpdateCoverPicture();
    }
    
    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AlbumModel.CoverTexture)) return;            
        GtkDispatch.InvokeIdle(UpdateCoverPicture);
    }

    private void UpdateCoverPicture()
    {
        _coverPicture.SetPaintable(Model?.CoverTexture);
    }
    
    private static void DragOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
        var widget = (AlbumListItem)sender.GetWidget()!;
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
    
    private static ContentProvider DragOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (AlbumListItem)sender.GetWidget()!;
        var wrapper = GId.NewForId(widget.Model!.Album.Id);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }    
}