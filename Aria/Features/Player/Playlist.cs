using GObject;
using Gtk;

namespace Aria.Features.Player;

[Subclass<Stack>]
[Template<AssemblyResource>("Aria.Features.Player.Playlist.ui")]
public partial class Playlist
{
    private bool _initialized;

    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        SetVisibleChildName("empty-playlist-page");
    }
}