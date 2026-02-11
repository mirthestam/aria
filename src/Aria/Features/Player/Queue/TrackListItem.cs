using System.ComponentModel;
using Aria.Core;
using Aria.Infrastructure;
using Gdk;
using Gio;
using GObject;
using Gtk;

namespace Aria.Features.Player.Queue;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.Queue.TrackListItem.ui")]
public partial class TrackListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("composer-label")] private Label _composerLabel;
    [Connect("duration-label")] private Label _durationLabel;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    
    [Connect("gesture-click")] private GestureClick _gestureClick;    
    [Connect("popover-menu")] private PopoverMenu _popoverMenu;

    public QueueTrackModel? Model { get; private set; }

    public void Bind(QueueTrackModel model)
    {
        if (Model != null)
        {
            // ListItems can be reused by GTK for different models.
            Model.PropertyChanged -= ModelOnPropertyChanged;
        }

        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;

        _titleLabel.SetLabel(model.TitleText);
        _subTitleLabel.SetLabel(model.SubTitleText);
        _composerLabel.SetLabel(model.ComposersText);
        _subTitleLabel.Visible = !string.IsNullOrEmpty(model.SubTitleText);
        _composerLabel.Visible = !string.IsNullOrEmpty(model.ComposersText);
        _durationLabel.SetLabel(model.DurationText);
        
        _gestureClick.OnPressed += GestureClickOnOnPressed;        

        ConfigureContextMenu();
        SetCoverPicture();
    }
    
    private void GestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        var rect = new Rectangle
        {
            X = (int)Math.Round(args.X),
            Y = (int)Math.Round(args.Y),
        };

        _popoverMenu.SetPointingTo(rect);

        if (!_popoverMenu.Visible)
            _popoverMenu.Popup();
    }
    
    private void ConfigureContextMenu()
    {
        var menu = Menu.NewWithProperties([]);
        menu.AppendItem(MenuItem.New("Remove", $"queue.delete-selection"));
        menu.AppendItem(MenuItem.New("Track Details", $"queue.show-track"));        
        menu.AppendItem(MenuItem.New("Show Album", $"queue.show-album"));        
        
        var playlistSection = Menu.NewWithProperties([]);
        playlistSection.AppendItem(MenuItem.New("Clear", $"{AppActions.Queue.Key}.{AppActions.Queue.Clear.Action}"));
        
        menu.AppendSection(null, playlistSection);
        
        _popoverMenu.SetMenuModel(menu);
    }

    private void SetCoverPicture()
    {
        if (Model?.CoverTexture == null) return;
        _coverPicture.SetPaintable(Model.CoverTexture);
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(QueueTrackModel.CoverTexture)) return;

        GLib.Functions.TimeoutAdd(0, 0, () =>
        {
            SetCoverPicture();
            return false;
        });
    }
}