using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aria.Core.Library;
using Gdk;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Browser.Artist;

[Subclass<Object>]
public sealed partial class AlbumModel : INotifyPropertyChanged
{
    public AlbumModel(AlbumInfo album, ArtistInfo artist) : this()
    {
        Album = album;
        Artist = artist;
    }

    public AlbumInfo Album { get; }
    public ArtistInfo Artist { get; }

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