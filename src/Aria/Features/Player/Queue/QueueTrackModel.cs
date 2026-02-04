using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Gdk;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Player.Queue;

[Subclass<Object>]
public partial class QueueTrackModel : INotifyPropertyChanged
{
    public QueueTrackModel(QueueTrackInfo queueTrack) : this()
    {
        var track = queueTrack.Track;
        TitleText = track?.Title ?? "Unnamed track";
        if (track?.Work?.ShowMovement ?? false)
            // For  these kind of works, we ignore the
            TitleText = $"{track.Work.MovementName} ({track.Work.MovementNumber} {track.Title} ({track.Work.Work})";
        
        var credits = track?.CreditsInfo;
        
        if (credits != null)
        {
            var artists = string.Join(", ", credits.OtherArtists.Select(x => x.Artist.Name));
        
            var details = new List<string>();
            var conductors = string.Join(", ", credits.Conductors.Select(x => x.Artist.Name));
            if (!string.IsNullOrEmpty(conductors))
                details.Add($"{conductors}");
        
            ComposersText = string.Join(", ", credits.Composers.Select(x => x.Artist.Name));
        
            SubTitleText = artists;
            if (details.Count > 0) SubTitleText += $" ({string.Join(", ", details)})";
        }
        
        if (queueTrack.Track.Duration == TimeSpan.Zero)
        {
            DurationText = "—:—";
        }
        else
        {
            DurationText = queueTrack.Track.Duration.TotalHours >= 1
                ? queueTrack.Track.Duration.ToString(@"h\:mm\:ss")
                : queueTrack.Track.Duration.ToString(@"mm\:ss");
        }
        
        QueueTrackId = queueTrack.Id;
        AlbumId = queueTrack.Track.AlbumId;
        TrackId = queueTrack.Track.Id;
        Position = queueTrack.Position;
    }
    
    public int Position { get; set; }
    
    public Id QueueTrackId { get; set; }
    
    public Id AlbumId { get; set; }
    
    public Id TrackId { get; set; }
    
    public string TitleText { get; set; }
    
    public string SubTitleText { get; set; }
    
    public string ComposersText { get; set; }
    
    public string DurationText { get; set; }
    
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