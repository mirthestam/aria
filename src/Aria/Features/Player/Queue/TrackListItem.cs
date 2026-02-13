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

    public QueueTrackModel? Model { get; private set; }
    
    public void Bind(QueueTrackModel model)
    {
        if (Model != null)
        {
            // ListItems can be reused by GTK for different models.
            _coverPicture.SetPaintable(null);
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
        
        UpdateCoverPicture();
    }
    
    private void UpdateCoverPicture()
    {
        _coverPicture.SetPaintable(Model?.CoverTexture);
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(QueueTrackModel.CoverTexture)) return;
        GtkDispatch.InvokeIdle(UpdateCoverPicture);
    }
}