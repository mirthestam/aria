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
    public static AlbumModel NewFor(AlbumInfo album)
    {
        var model = NewWithProperties([]);
        model.Album = album;
        return model;
    }
    
    public AlbumInfo Album { get; private set; }

    public Texture? CoverTexture
    {
        get;
        set
        {
            if (ReferenceEquals(field, value)) return;

            // This model owns the texture once assigned.
            field?.Dispose();

            field = value;
            OnPropertyChanged();
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}