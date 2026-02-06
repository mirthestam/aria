using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aria.Core.Library;
using Gdk;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Browser.Artist;

[Subclass<Object>]
public partial class AlbumModel : INotifyPropertyChanged
{
    public static AlbumModel NewFor(AlbumInfo album, ArtistInfo artist)
    {
        var model = NewWithProperties([]);
        model.Album = album;
        model.Artist = artist;
        return model;
    }
    
    public AlbumInfo Album { get; private set; }
    public ArtistInfo Artist { get; private set; }

    public Texture? CoverTexture
    {
        get;
        set => SetField(ref field, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }
}