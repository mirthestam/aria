using System.ComponentModel;
using Aria.Core;
using Gdk;
using Gio;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Albums;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Albums.AlbumsAlbumListItem.ui")]
public partial class AlbumsAlbumListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    
    public AlbumsAlbumModel? Model { get; private set; }

    public void Initialize(AlbumsAlbumModel model)
    {
        if (Model != null) Model.PropertyChanged -= ModelOnPropertyChanged;

        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;

        var artistsLine = string.Join(", ", model.Album.CreditsInfo.AlbumArtists.Select(a => a.Name));

        _titleLabel.SetLabel(model.Album.Title);
        _subTitleLabel.SetLabel(artistsLine);
        
        SetCoverPicture();
    }
    
    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AlbumsAlbumModel.CoverTexture)) return;
        
        GLib.Functions.TimeoutAdd(0, 0, () =>
        {
            SetCoverPicture();
            return false;
        });
    }

    private void SetCoverPicture()
    {
        if (Model?.CoverTexture == null) return;
        _coverPicture.SetPaintable(Model.CoverTexture);
    }
}