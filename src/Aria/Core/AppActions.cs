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
        public static readonly string Key = "aria-browser";
        
        public static readonly AppAction Search = new("search", "<Control>f");        
        public static readonly AppAction AllAlbums = new("show-all-albums", "<Control>h");
        public static readonly AppAction Playlists = new("show-playlists", "<Control>p");        
        public static readonly AppAction ShowAlbum = new("show-album");
        public static readonly AppAction ShowAlbumForArtist = new("show-album-for-artist");
        public static readonly AppAction ShowArtist = new("show-artist");        
        public static readonly AppAction ShowTrack = new("show-track");
    }

    public static class Queue
    {
        public static readonly string Key = "aria-queue";
        
        public static readonly AppAction Clear = new("clear", "<Control>Delete");
        
        public static readonly AppAction EnqueueDefault = new("enqueue-default");
        public static readonly AppAction EnqueueReplace = new("enqueue-replace");
        public static readonly AppAction EnqueueNext = new("enqueue-next");
        public static readonly AppAction EnqueueEnd = new("enqueue-end");
        
        public static readonly AppAction RemoveTrack = new("remove-track", "delete");
        
        public static readonly AppAction Shuffle = new("shuffle", "<Control>s");
        public static readonly AppAction Repeat = new("repeat", "");
        public static readonly AppAction Consume = new("consume", "<Control>r");
    }

    public static class Player
    {
        public static readonly string Key = "aria-player";
        
        public static readonly AppAction PlayPause = new("play-pause", "<Control>space");        
        public static readonly AppAction Stop = new("stop", "<Control><Shift>space");
        
        public static readonly AppAction Next = new("next", "<Control>period");
        public static readonly AppAction Previous = new("previous", "<Control>comma");
    }
}