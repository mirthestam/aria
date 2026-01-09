namespace Aria.Core.Player;

[Flags]
public enum PlayerStateChangedFlags
{
    None = 0,
    State = 1 << 0,
    Volume = 1 << 1,
    Progress = 1 << 2, 
    CurrentSong = 1 << 3, 
    XFade = 1 << 4,
    All = ~0
}