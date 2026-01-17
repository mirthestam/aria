namespace Aria.Infrastructure.Tagging;

/// <summary>
/// Constants for names of tags as defined by the MPD best practices as documented
/// https://mpd.readthedocs.io/en/latest/protocol.html#tags
/// </summary>
public static class MPDTags
{
    public const string File = "file";
    public const string Duration = "duration";
    public const string Title = "title";
    public const string Name = "name";
    public const string Genre = "genre";
    public const string Comment = "comment";
    public const string Id = "id";
    public const string Date = "date";

    // Album
    public const string Album = "album";
    public const string AlbumArtist = "albumartist";
    public const string Track = "track";
    public const string Disc = "disc";
    public const string Pos = "pos";

    // Artists
    public const string Artist = "artist";
    public const string Composer = "composer";
    public const string Conductor = "conductor";
    public const string Performer = "performer";
    public const string Ensemble = "ensemble";

    // Work
    public const string Work = "work";
    public const string Movement = "movement";
    public const string MovementNumber = "movementnumber";
    public const string ShowMovement = "showmovement";
    
    // Recording
    public const string Location = "location";
}