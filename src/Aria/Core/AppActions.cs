namespace Aria.Core;

public record AppAction(string Action, string Accelerator = "");

public static class AppActions
{
    public static class Window
    {
        public const string Key = "win";
        
        public static readonly AppAction Disconnect = new("disconnect", "<Control>d");
        public static readonly AppAction About = new("about", "F1");
    }

    public static class Browser
    {
        public static readonly string Key = "browser";
        
        public static readonly AppAction Search = new("search", "<Control>f");        
        public static readonly AppAction AllAlbums = new("show-all-albums", "<Control>h");
        public static readonly AppAction ShowAlbum = new("show-album");
        public static readonly AppAction ShowAlbumForArtist = new("show-album-for-artist");
        public static readonly AppAction ShowArtist = new("show-artist");        
    }

    public static class Queue
    {
        public static readonly string Key = "queue";
        
        public static readonly AppAction Clear = new("clear", "<Control>n");
        
        public static readonly AppAction EnqueueDefault = new("enqueue-default");
        public static readonly AppAction EnqueueReplace = new("enqueue-replace");
        public static readonly AppAction EnqueueNext = new("enqueue-next");
        public static readonly AppAction EnqueueEnd = new("enqueue-end");
    }

    public static class Player
    {
        public static readonly string Key = "player";
        
        public static readonly AppAction PlayPause = new("play-pause", "<Control>space");        
        public static readonly AppAction Stop = new("stop", "<Control><Shift>space");
        
        public static readonly AppAction Next = new("next", "<Control>period");
        public static readonly AppAction Previous = new("previous", "<Control>comma");
    }
}