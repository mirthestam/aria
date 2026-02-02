namespace Aria.Core;

public record Accelerator(string Accels, string Name);

public static class Accelerators
{
    public static class Window
    {
        public const string Key = "win";
        
        public static readonly Accelerator Disconnect = new("<Control>d", "disconnect");
        public static readonly Accelerator About = new("F1", "about");
    }

    public static class Browser
    {
        public static string Key = "browser";
        
        public static readonly Accelerator Search = new("<Control>f", "search");        
        public static readonly Accelerator AllAlbums = new("<Control>h", "all-albums");
        public static readonly Accelerator ShowArtist = new("", "show-artist");
    }

    public static class Queue
    {
        public static string Key = "queue";
        
        public static readonly Accelerator Clear = new("<Control>n", "clear");
    }

    public static class Player
    {
        public static string Key = "player";
        
        public static readonly Accelerator PlayPause = new("<Control>space", "play-pause");        
        public static readonly Accelerator Stop = new("<Control><Shift>space", "stop");
        
        public static readonly Accelerator Next = new("<Control>period", "next");
        public static readonly Accelerator Previous = new("<Control>comma", "previous");
    }
}