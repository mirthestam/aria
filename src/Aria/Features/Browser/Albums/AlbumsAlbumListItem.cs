using System.ComponentModel;
using Aria.Infrastructure;
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

    public void Bind(AlbumsAlbumModel model)
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
        if (e.PropertyName != nameof(AlbumsAlbumModel.CoverTexture)) return;            
        GtkDispatch.InvokeIdle(UpdateCoverPicture);
    }

    private void UpdateCoverPicture()
    {
        _coverPicture.SetPaintable(Model?.CoverTexture);
    }
}