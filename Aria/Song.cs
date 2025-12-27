namespace Aria;

public record Song
{
    public Id Id { get; init; }
    
    public TimeSpan Duration { get; init; }
    
    public string AlbumArtist { get; init; }
    
    public string Artist { get; init; }
    
    public string Composer { get; init; }
    
    public string Title { get; init; }
}