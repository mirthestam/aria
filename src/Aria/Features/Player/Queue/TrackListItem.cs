using System.ComponentModel;
using Aria.Core.Library;
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

    public QueueTrackModel? Model { get; private set; }

    public void Initialize(QueueTrackModel model)
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

        ConfigureContextMenu();
        SetCoverPicture();
    }

    private void ConfigureContextMenu()
    {
        // var argument = Variant.NewArray(VariantType.String, [Variant.NewString(Model.Album.Id!.ToString())]);
        //
        // var menu = new Menu();
        // menu.AppendItem(MenuItem.New("Show Album", $"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}::{Model.Album.Id}"));
        //
        //
        // var enqueueMenu = new Menu();
        //
        // var replaceQueueItem = MenuItem.New("Play now (Replace queue)", null);
        // replaceQueueItem.SetActionAndTargetValue($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", argument);
        // enqueueMenu.AppendItem(replaceQueueItem);
        //
        // var playNextItem = MenuItem.New("Play after current track", null);
        // playNextItem.SetActionAndTargetValue($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", argument);
        // enqueueMenu.AppendItem(playNextItem);        
        //
        // var playLastItem = MenuItem.New("Add to queue", null);
        // playLastItem.SetActionAndTargetValue($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", argument);
        // enqueueMenu.AppendItem(playLastItem);        
        //
        // menu.AppendSection(null, enqueueMenu);
        //
        // _popoverMenu.SetMenuModel(menu);
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