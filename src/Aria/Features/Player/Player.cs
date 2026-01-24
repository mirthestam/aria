using Aria.Core.Player;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Player;

public delegate Task SeekRequestedAsyncHandler(TimeSpan position, CancellationToken cancellationToken);

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.Player.ui")]
public partial class Player
{
    [Connect("album-picture")] private Picture _coverPicture;
    [Connect("playback-controls")] private PlaybackControls _playbackControls;
    [Connect("playlist")] private Playlist.Playlist _playlist;
    
    public Playlist.Playlist Playlist => _playlist;

    public event SeekRequestedAsyncHandler? SeekRequested;    
    
    partial void Initialize()
    {
        _playbackControls.SeekRequested += PlaybackControlsOnSeekRequested;
    }

    private Task PlaybackControlsOnSeekRequested(TimeSpan position, CancellationToken cancellationToken)
    {
        return SeekRequested?.Invoke(position, cancellationToken) ?? Task.CompletedTask;
    }
    
    public void LoadCover(Texture texture)
    {
        _coverPicture.Visible = true;
        _coverPicture.SetPaintable(texture);
    }

    public void ClearCover()
    {
        _coverPicture.Visible = false;
        _coverPicture.SetPaintable(null);
    }

    public void SetProgress(TimeSpan progressElapsed, TimeSpan progressDuration)
    {
        _playbackControls.SetProgress(progressElapsed, progressDuration);
    }

    public void SetPlaylistInfo(int? orderCurrentIndex, int queueLength)
    {
        _playbackControls.SetPlaylistInfo(orderCurrentIndex, queueLength);
    }

    public void SetPlaybackState(PlaybackState playerState)
    {
        _playbackControls.SetPlaybackState(playerState);
    }
}