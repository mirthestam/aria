using System.ComponentModel;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Artist;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Artist.AlbumListItem.ui")]
public partial class AlbumListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;

    private AlbumModel? _model;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;

    public void Initialize(AlbumModel model)
    {
        if (_model != null) _model.PropertyChanged -= ModelOnPropertyChanged;

        _model = model;
        _model.PropertyChanged += ModelOnPropertyChanged;

        var artistsLine = string.Join(", ", model.Album.CreditsInfo.AlbumArtists.Select(a => a.Name));

        _titleLabel.SetLabel(model.Album.Title);
        _subTitleLabel.SetLabel(artistsLine);

        SetCoverPicture();
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AlbumModel.CoverTexture)) return;
        SetCoverPicture();
    }

    private void SetCoverPicture()
    {
        if (_model?.CoverTexture == null) return;
        _coverPicture.SetPaintable(_model.CoverTexture);
    }
}