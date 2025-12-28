using GObject;
using Gtk;

namespace Aria.Features.Player;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.MediaControls.ui")]
public partial class MediaControls
{
    private bool _initialized;
    [Connect("playback-start-button")] private Button _playbackStartButton;
    [Connect("skip-backward-button")] private Button _skipBackwardButton;
    [Connect("skip-forward-button")] private Button _skipForwardButton;

    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
    }
}