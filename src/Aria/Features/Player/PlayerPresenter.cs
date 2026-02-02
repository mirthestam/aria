using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Player.Playlist;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Player;

public partial class PlayerPresenter : IPresenter<Player>,  IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _aria;
    private readonly ILogger<PlayerPresenter> _logger;
    private readonly PlaylistPresenter _playlistPresenter;
    private readonly ResourceTextureLoader _resourceTextureLoader;
    private readonly IMessenger _messenger;
    
    private CancellationTokenSource? _coverArtCancellationTokenSource;
    
    public Player? View { get; set; }

    private SimpleAction _nextAction;
    private SimpleAction _prevAction;
    private SimpleAction _playPauseAction;
    private SimpleAction _stopAction;
    private SimpleAction _clearQueueAction;
    
    public PlayerPresenter(ILogger<PlayerPresenter> logger, IMessenger messenger, IAria aria,
        ResourceTextureLoader resourceTextureLoader, PlaylistPresenter playlistPresenter)
    {
        _messenger = messenger;
        _logger = logger;
        _resourceTextureLoader = resourceTextureLoader;
        _playlistPresenter = playlistPresenter;
        _aria = aria;
        messenger.RegisterAll(this);
    }
    
    public void Attach(Player player, AttachContext context)
    {
        View = player;
        View.SeekRequested += ViewOnSeekRequested;
        View.EnqueueRequested += ViewOnEnqueueRequested;

        _playlistPresenter.Attach(View.Playlist);
        
        var playerActionGroup = SimpleActionGroup.New();
        playerActionGroup.AddAction(_nextAction = SimpleAction.New(Accelerators.Player.Next.Name, null));
        playerActionGroup.AddAction(_prevAction = SimpleAction.New(Accelerators.Player.Previous.Name, null));
        playerActionGroup.AddAction(_playPauseAction = SimpleAction.New(Accelerators.Player.PlayPause.Name, null));
        playerActionGroup.AddAction(_stopAction = SimpleAction.New(Accelerators.Player.Stop.Name, null));
        context.InsertAppActionGroup(Accelerators.Player.Key, playerActionGroup);
        
        context.SetAccelsForAction($"{Accelerators.Player.Key}.{Accelerators.Player.Next.Name}", [Accelerators.Player.Next.Accels]);
        context.SetAccelsForAction($"{Accelerators.Player.Key}.{Accelerators.Player.Previous.Name}", [Accelerators.Player.Previous.Accels]);
        context.SetAccelsForAction($"{Accelerators.Player.Key}.{Accelerators.Player.PlayPause.Name}", [Accelerators.Player.PlayPause.Accels]);
        context.SetAccelsForAction($"{Accelerators.Player.Key}.{Accelerators.Player.Stop.Name}", [Accelerators.Player.Stop.Accels]);       
        
        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_clearQueueAction = SimpleAction.New(Accelerators.Queue.Clear.Name, null));
        context.InsertAppActionGroup(Accelerators.Queue.Key, queueActionGroup);
        context.SetAccelsForAction($"{Accelerators.Queue.Key}.{Accelerators.Queue.Clear.Name}", [Accelerators.Queue.Clear.Accels]);        
        
        _nextAction.OnActivate += NextActionOnOnActivate;
        _prevAction.OnActivate += PrevActionOnOnActivate;        
        _playPauseAction.OnActivate += PlayPauseActionOnOnActivate;
        _stopAction.OnActivate += StopActionOnOnActivate;
        _clearQueueAction.OnActivate += ClearQueueActionOnOnActivate;
    }

    private async void ClearQueueActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _aria.Queue.ClearAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to stop playback"));
        }
    }

    private async void StopActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _aria.Player.StopAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to stop playback"));
        }
    }

    private async void PrevActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        { 
            await _aria.Player.PreviousAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to previous track"));
        }
    }

    private async void NextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _aria.Player.NextAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to next track"));
        }
    }    

    private async void PlayPauseActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            switch (_aria.Player.State)
            {
                case PlaybackState.Paused:
                    await _aria.Player.ResumeAsync();
                    break;
                case PlaybackState.Stopped:
                    var currentTrack = _aria.Queue.CurrentTrack;
                    await _aria.Player.PlayAsync(currentTrack?.Position ?? 0);
                    break;
                case PlaybackState.Playing:
                    await _aria.Player.PauseAsync();
                    break;
            }
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to play/pause track"));
        }
    }
    
    
    private async void ViewOnEnqueueRequested(object? sender, Id id)
    {
        try
        {
            var info = await _aria.Library.GetItemAsync(id);
            if (info == null) return;

            _ = _aria.Queue.EnqueueAsync(info, IQueue.DefaultEnqueueAction);
        }
        catch (Exception exception)
        {
            _messenger.Send(new ShowToastMessage("Could not enqueue"));
            LogCouldNotEnqueue(exception);
        }
    }

    private async Task ViewOnSeekRequested(TimeSpan position, CancellationToken cancellationToken)
    {
        await _aria.Player.SeekAsync(position, cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _ = RefreshCover(cancellationToken);
        
        Refresh(QueueStateChangedFlags.All);
        Refresh(PlayerStateChangedFlags.All);

        await _playlistPresenter.RefreshAsync();

    }

    public void Reset()
    {
        _playlistPresenter.Reset();
        AbortRefreshCover();
        
        GLib.Functions.IdleAdd(0, () =>
        {
            View?.ClearCover();
            return false;
        });        
    }
    
    public void Receive(PlayerStateChangedMessage message)
    {
        Refresh(message.Value);
    }

    public void Receive(QueueStateChangedMessage message)
    {
        Refresh(message.Value);
    }

    private void Refresh(PlayerStateChangedFlags flags)
    {
        if (flags.HasFlag(PlayerStateChangedFlags.PlaybackState))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                View?.SetPlaybackState(_aria.Player.State);
                return false;
            });            
        }
        if (flags.HasFlag(PlayerStateChangedFlags.Progress))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                View?.SetProgress(_aria.Player.Progress.Elapsed, _aria.Player.Progress.Duration);
                return false;
            });            
        }
    }

    private void Refresh(QueueStateChangedFlags flags)
    {
        if (flags.HasFlag(QueueStateChangedFlags.Id) || flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                View?.SetPlaylistInfo(_aria.Queue.Order.CurrentIndex, _aria.Queue.Length);
                return false;
            });            
        }
        
        if (!flags.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;

        GLib.Functions.IdleAdd(0, () =>
        {
            _prevAction.SetEnabled(_aria.Queue.Order.CurrentIndex > 0);
            _nextAction.SetEnabled(_aria.Queue.Order.HasNext);
            _playPauseAction.SetEnabled(_aria.Queue.Length > 0);
            return false;
        });                
        
        _ = RefreshCover();
    }
    
    private void AbortRefreshCover()
    {
        _coverArtCancellationTokenSource?.Cancel();
        _coverArtCancellationTokenSource?.Dispose();
        _coverArtCancellationTokenSource = null;
    }

    private async Task RefreshCover(CancellationToken externalCancellationToken = default)
    {
        AbortRefreshCover();
        
        // Create a new cancellation token source that is optionally linked to an external token.
        // This allows cover loading to be cancelled both internally (e.g., when a new track starts)
        // and externally (e.g., when the component cancels connection via the cancellation token passed to ConnectAsync).
        // The linked token ensures that cancelling either source will cancel the cover loading operation.
        _coverArtCancellationTokenSource = externalCancellationToken != CancellationToken.None 
            ? CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken) 
            : new CancellationTokenSource();
            
        var cancellationToken = _coverArtCancellationTokenSource.Token;            
        
        try
        {
            var track = _aria.Queue.CurrentTrack;
            if (track == null)
            {
                GLib.Functions.IdleAdd(0, () =>
                {
                    View?.ClearCover();
                    return false;
                });
                return;
            };

            var coverInfo = track.Track.Assets.FrontCover;
            var texture = await _resourceTextureLoader.LoadFromAlbumResourceAsync(coverInfo?.Id ?? Id.Empty, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            if (texture == null) return;
            
            GLib.Functions.IdleAdd(0, () =>
            {
                View?.LoadCover(texture);
                return false;
            });            
        }
        catch (OperationCanceledException)
        {
            // Expected when a new cover starts loading
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested) LogFailedToLoadAlbumCover(e);
        }
    }
    
    [LoggerMessage(LogLevel.Error, "Player action failed: {action}")]
    partial void PlayerActionFailed(Exception e, string? action);    
    
    [LoggerMessage(LogLevel.Error, "Failed to load album cover")]
    partial void LogFailedToLoadAlbumCover(Exception e);
    
    [LoggerMessage(LogLevel.Error, "Could not enqueue")]
    partial void LogCouldNotEnqueue(Exception e);    
}