using Aria.Core;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Gdk;
using Gio;
using GObject;
using Gtk;

namespace Aria.Features.Player;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.Player.ui")]
public partial class Player
{
    [Connect("playback-controls")] private PlaybackControls _playbackControls;
    [Connect("playlist")] private Playlist.Playlist _playlist;
    [Connect("album-picture")] private Picture _coverPicture;

    public SimpleAction NextAction { get; private set; }
    public SimpleAction PrevAction { get; private set; }

    partial void Initialize()
    {
        var actionGroup = SimpleActionGroup.New();

        NextAction = SimpleAction.New("next", null);
        actionGroup.AddAction(NextAction);

        PrevAction = SimpleAction.New("prev", null);
        actionGroup.AddAction(PrevAction);

        InsertActionGroup("player", actionGroup);
    }

    public Playlist.Playlist Playlist => _playlist;

    public void QueueStateChanged(QueueStateChangedFlags flags, IPlaybackApi api)
    {
        _playbackControls.QueueStateChanged(flags, api);
    }
    
    public void PlayerStateChanged(PlayerStateChangedFlags flags, IPlaybackApi api)
    {
        _playbackControls.PlayerStateChanged(flags, api);
    }

    public void LoadCover(Texture texture)
    {
        _coverPicture.SetPaintable(texture);
    }
}