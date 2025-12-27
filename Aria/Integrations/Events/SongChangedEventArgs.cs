namespace Aria.Integrations;

public class SongChangedEventArgs(Id? newSongId) : EventArgs
{
    public Id? NewSongId { get; set; } = newSongId;
}