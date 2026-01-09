using System.ComponentModel;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Albums;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Albums.AlbumsAlbumListItem.ui")]
public partial class AlbumsAlbumListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("title-label")] private Label _titleLabel;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    
    private AlbumsAlbumModel? _model;
    
    public void Initialize(AlbumsAlbumModel model)
    {
        if (_model != null)
        {
            _model.PropertyChanged -= ModelOnPropertyChanged;
        }

        _model = model;
        _model.PropertyChanged += ModelOnPropertyChanged;
        
        var artistsLine = string.Join(", ", model.Album.CreditsInfo.AlbumArtists.Select(a => a.Name));
        
        _titleLabel.SetLabel(model.Album.Title);
        _subTitleLabel.SetLabel(artistsLine);
        
        SetCoverPicture();        
    }
    
    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AlbumsAlbumModel.CoverTexture)) return;
        SetCoverPicture();
    }

    private void SetCoverPicture()
    {
        if (_model?.CoverTexture == null) return;
        _coverPicture.SetPaintable(_model.CoverTexture);
    }    
}