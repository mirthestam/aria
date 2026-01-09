using Gio;
using GObject;
using Gtk;

namespace Aria.Features.Player;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.MediaControls.ui")]
public partial class MediaControls
{
    [Connect("playback-start-button")] private Button _playbackStartButton;
    [Connect("skip-backward-button")] private Button _skipBackwardButton;
    [Connect("skip-forward-button")] private Button _skipForwardButton;
}