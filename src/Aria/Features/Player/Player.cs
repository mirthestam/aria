using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Player;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;
using Object = GObject.Object;

namespace Aria.Features.Player;

public delegate Task SeekRequestedAsyncHandler(TimeSpan position, CancellationToken cancellationToken);

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.Player.ui")]
public partial class Player
{
    [Connect("album-picture")] private Picture _coverPicture;
    [Connect("playback-controls")] private PlaybackControls _playbackControls;
    [Connect("playlist")] private Queue.Queue _queue;
    
    public Queue.Queue Queue => _queue;

    public event SeekRequestedAsyncHandler? SeekRequested;    
    public event EventHandler<int>? VolumeChanged;
    
    public event EventHandler<Id> EnqueueRequested;    
    
    partial void Initialize()
    {
        _playbackControls.SeekRequested += PlaybackControlsOnSeekRequested;
        _playbackControls.VolumeChanged += PlaybackControlsOnVolumeChanged;
        
        // Add the playback drop target
        var type = GObject.Type.Object;        
        var idWrapperDropTarget = DropTarget.New(type, DragAction.Copy);
        idWrapperDropTarget.OnDrop  += IdWrapperDropTargetOnOnDrop;
        AddController(idWrapperDropTarget);
    }

    private void PlaybackControlsOnVolumeChanged(object? sender, int e) => VolumeChanged?.Invoke(sender, e);

    private bool IdWrapperDropTargetOnOnDrop(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something onto player.
        var value = args.Value.GetObject();
        if (value is not GId gId) return false;
        
        EnqueueRequested(this, gId.Id);

        return true;
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

    public void SetProgress(PlaybackProgress progress)
    {
        _playbackControls.SetProgress(progress);
    }

    public void SetPlaylistInfo(int? orderCurrentIndex, int queueLength)
    {
        _playbackControls.SetPlaylistInfo(orderCurrentIndex, queueLength);
    }

    public void SetPlaybackState(PlaybackState playerState)
    {
        _playbackControls.SetPlaybackState(playerState);
    }

    public void SetVolume(int? playerVolume)
    {
        _playbackControls.SetVolume(playerVolume);
    }
}