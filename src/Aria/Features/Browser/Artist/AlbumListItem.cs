using System.ComponentModel;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Artist;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Artist.AlbumListItem.ui")]
public partial class AlbumListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    
    public AlbumModel? Model { get; private set; }
    
    public void Initialize(AlbumModel model)
    {
        if (Model != null)
        {
            _coverPicture.SetPaintable(null);
            Model.PropertyChanged -= ModelOnPropertyChanged;
        }
        
        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;

        // TODO: I can sort here now on role with priority
        var artistsLine = string.Join(", ", model.Album.CreditsInfo.AlbumArtists.Select(a => a.Artist.Name));

        _titleLabel.SetLabel(model.Album.Title);
        _subTitleLabel.SetLabel(artistsLine);
        
        UpdateCoverPicture();
    }
    
    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AlbumModel.CoverTexture)) return;
        UpdateCoverPicture();
    }
 
    private void UpdateCoverPicture()
    {
        _coverPicture.SetPaintable(Model?.CoverTexture);
    }
}