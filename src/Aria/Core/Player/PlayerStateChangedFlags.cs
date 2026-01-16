namespace Aria.Core.Player;

[Flags]
public enum PlayerStateChangedFlags
{
    None = 0,
    PlaybackState = 1 << 0,
    Volume = 1 << 1,
    Progress = 1 << 2, 
    XFade = 1 << 3,
    All = ~0
}