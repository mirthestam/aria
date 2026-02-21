namespace Aria.Core.Library;

[Flags]
public enum LibraryChangedFlags
{
    None = 0,
    
    /// <summary>
    /// Common elements (Albums, Artists, etc)
    /// </summary>
    Library = 1 << 0,
    
    /// <summary>
    /// Playlists (Stored)
    /// </summary>
    Playlists = 1 << 1
}