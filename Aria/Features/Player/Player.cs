using Aria.Core;
using Aria.Core.Player;
using GObject;
using Gtk;

namespace Aria.Features.Player;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.Player.ui")]
public partial class Player
{
    [Connect("playback-controls")] private PlaybackControls _playbackControls;

    public void PlayerStateChanged(PlayerStateChangedFlags flags, IPlaybackApi api)
    {
        _playbackControls.PlayerStateChanged(flags, api);
    }
}