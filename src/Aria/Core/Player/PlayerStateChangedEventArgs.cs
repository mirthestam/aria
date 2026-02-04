namespace Aria.Core.Player;

public class PlayerStateChangedEventArgs : EventArgs
{
    public PlayerStateChangedFlags Flags { get; }

    public PlayerStateChangedEventArgs(PlayerStateChangedFlags flags)
    {
        Flags = flags;
    }
}