namespace Aria.Integrations;

public class Status
{
    public class SongStatus
    {
        public Id? Id { get; internal set; }

        public TimeSpan Elapsed { get; internal set;  }
    
        public TimeSpan Duration { get; internal set;  }
    
        public TimeSpan Remaining => Duration - Elapsed;
        
        /// <summary>
        /// i.e.  16, 24 (-bit)
        /// </summary>
        public int AudioBits { get; internal set; }
        
        /// <summary>
        /// i.e. 2, 6 (channels)
        /// </summary>
        public int AudioChannels { get; internal set; }
        
        /// <summary>
        /// i.e. 44100, 96000 (hz)
        /// </summary>
        public int AudioSampleRate { get; internal set; }
        
        /// <summary>
        ///  i.e. 320 (kbps)
        /// </summary>
        public int Bitrate { get; internal set; }        
    }

    public class PlayerStatus
    {
        public Id Id { get; internal set; }
        
        public int? Volume { get; internal set; }
        
        public bool SupportsVolume { get; internal set; }
        
        public PlayerState State {get; internal set;  }
        
        /// <summary>
        /// The number of seconds to Crossfaded between song changes 
        /// </summary>
        public int? XFade { get; internal set; }
        
        /// <summary>
        /// Whether this player supports crossfading
        /// </summary>
        public bool CanXFade { get; internal set; }
    }

    public class PlaylistStatus
    {
        public Id Id { get; internal set; }
        
        public Id? NextSongId { get; internal set; }
        
        public int CurrentSongIndex { get; internal set; }
        
        public int Length { get; internal set; }

        public bool CanShuffle { get; internal set; }
        public bool ShuffleEnabled { get; internal set; }        
        
        public bool CanConsume { get; internal set; }
        public bool ConsumeEnabled { get; internal set; }
        
        public bool CanRepeat { get; internal set; }
        public bool RepeatEnabled { get; internal set; }
        public bool SingleEnabled { get; internal set; }
        
    }
    
    public PlayerStatus Player { get; } = new();    
    public SongStatus Song { get; } = new();
    public PlaylistStatus Playlist { get; } = new();    
    
    public static Status Empty => new Status();
}