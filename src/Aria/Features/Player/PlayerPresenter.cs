using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Player.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using GLib;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;
using TimeSpan = System.TimeSpan;

namespace Aria.Features.Player;

public partial class PlayerPresenter : IPresenter<Player>,  IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _aria;
    private readonly IAriaControl _ariaControl;
    private readonly ILogger<PlayerPresenter> _logger;
    private readonly QueuePresenter _queuePresenter;
    private readonly ResourceTextureLoader _resourceTextureLoader;
    private readonly IMessenger _messenger;
    
    private CancellationTokenSource? _coverArtCancellationTokenSource;
    
    public Player? View { get; set; }

    private SimpleAction _playerNextTrackAction;
    private SimpleAction _playerPreviousTrackAction;
    private SimpleAction _playerPlayPauseAction;
    private SimpleAction _playerStopAction;
    
    private SimpleAction _queueEnqueueDefaultAction;
    private SimpleAction _queueEnqueueReplaceAction;
    private SimpleAction _queueEnqueueNextAction;
    private SimpleAction _queueEnqueueEndAction;
    private SimpleAction _queueClearAction;    

    public PlayerPresenter(ILogger<PlayerPresenter> logger, IMessenger messenger, IAria aria,
        ResourceTextureLoader resourceTextureLoader, QueuePresenter queuePresenter, IAriaControl ariaControl)
    {
        _messenger = messenger;
        _logger = logger;
        _resourceTextureLoader = resourceTextureLoader;
        _queuePresenter = queuePresenter;
        _ariaControl = ariaControl;
        _aria = aria;
        messenger.RegisterAll(this);
    }
    
    public void Attach(Player player, AttachContext context)
    {
        View = player;
        View.SeekRequested += ViewOnSeekRequested;
        View.EnqueueRequested += ViewOnEnqueueRequested;

        _queuePresenter.Attach(View.Queue);
        
        var playerActionGroup = SimpleActionGroup.New();
        playerActionGroup.AddAction(_playerNextTrackAction = SimpleAction.New(AppActions.Player.Next.Action, null));
        playerActionGroup.AddAction(_playerPreviousTrackAction = SimpleAction.New(AppActions.Player.Previous.Action, null));
        playerActionGroup.AddAction(_playerPlayPauseAction = SimpleAction.New(AppActions.Player.PlayPause.Action, null));
        playerActionGroup.AddAction(_playerStopAction = SimpleAction.New(AppActions.Player.Stop.Action, null));
        context.InsertAppActionGroup(AppActions.Player.Key, playerActionGroup);
        
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Next.Action}", [AppActions.Player.Next.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Previous.Action}", [AppActions.Player.Previous.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.PlayPause.Action}", [AppActions.Player.PlayPause.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Player.Key}.{AppActions.Player.Stop.Action}", [AppActions.Player.Stop.Accelerator]);       
        
        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_queueClearAction = SimpleAction.New(AppActions.Queue.Clear.Action, null));
        queueActionGroup.AddAction(_queueEnqueueDefaultAction = SimpleAction.New(AppActions.Queue.EnqueueDefault.Action, VariantType.NewArray(VariantType.String)));        
        queueActionGroup.AddAction(_queueEnqueueReplaceAction = SimpleAction.New(AppActions.Queue.EnqueueReplace.Action, VariantType.NewArray(VariantType.String)));
        queueActionGroup.AddAction(_queueEnqueueNextAction = SimpleAction.New(AppActions.Queue.EnqueueNext.Action, VariantType.NewArray(VariantType.String)));
        queueActionGroup.AddAction(_queueEnqueueEndAction = SimpleAction.New(AppActions.Queue.EnqueueEnd.Action, VariantType.NewArray(VariantType.String)));        
        context.InsertAppActionGroup(AppActions.Queue.Key, queueActionGroup);
        context.SetAccelsForAction($"{AppActions.Queue.Key}.{AppActions.Queue.Clear.Action}", [AppActions.Queue.Clear.Accelerator]);        
        
        _playerNextTrackAction.OnActivate += PlayerNextTrackActionOnOnActivate;
        _playerPreviousTrackAction.OnActivate += PlayerPreviousTrackActionOnOnActivate;        
        _playerPlayPauseAction.OnActivate += PlayerPlayPauseActionOnOnActivate;
        _playerStopAction.OnActivate += PlayerStopActionOnOnActivate;
        
        _queueClearAction.OnActivate += QueueClearActionOnOnActivate;
        
        _queueEnqueueDefaultAction.OnActivate += DefaultQueueEnqueueActionOnOnActivate;
        _queueEnqueueEndAction.OnActivate += QueueEnqueueEndActionOnOnActivate;
        _queueEnqueueNextAction.OnActivate += QueueEnqueueNextActionOnOnActivate;
        _queueEnqueueReplaceAction.OnActivate += PlayActionOnOnActivate;        
    }

    private void DefaultQueueEnqueueActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(IQueue.DefaultEnqueueAction, args);
    private void QueueEnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.EnqueueEnd, args);
    private void QueueEnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.EnqueueNext, args);
    private void PlayActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.Replace, args);

    private async void EnqueueHandler(EnqueueAction enqueueAction, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var serializedIds = args.Parameter!.GetStrv(out _);
            var ids = serializedIds.Select(_ariaControl.Parse).ToArray();
            
            // Enqueue the items by id
            await EnqueueIds(enqueueAction, ids).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToEnqueueTracks(_logger, e);
            _messenger.Send(new ShowToastMessage($"Failed to enqueue tracks."));
        }        
    }
    
    private async Task EnqueueIds(EnqueueAction action, Id[] ids)
    {
        var items = new List<Info>();
        foreach (var id in ids)
        {
            // Would be great to have 'GetItems' instead of foreach here.
            var item =await _aria.Library.GetItemAsync(id).ConfigureAwait(false);
            if (item == null) continue;
            items.Add(item);
        }

        await _aria.Queue.EnqueueAsync(items, action).ConfigureAwait(false);
        
        switch (action)
        {
            case EnqueueAction.Replace:
                _messenger.Send(new ShowToastMessage($"Playing tracks."));
                break;
            case EnqueueAction.EnqueueNext:
                _messenger.Send(new ShowToastMessage($"Playing tracks Next."));
                break;
            case EnqueueAction.EnqueueEnd:
                _messenger.Send(new ShowToastMessage($"Added tracks to end of queue."));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }    
    
    private async void QueueClearActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
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

    private async void PlayerStopActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
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

    private async void PlayerPreviousTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
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

    private async void PlayerNextTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
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

    private async void PlayerPlayPauseActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
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
        Refresh(QueueStateChangedFlags.All);
        Refresh(PlayerStateChangedFlags.All);

        await _queuePresenter.RefreshAsync(cancellationToken);
        await RefreshCover(cancellationToken);        
    }

    public void Reset()
    {
        _queuePresenter.Reset();
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
            _playerPreviousTrackAction.SetEnabled(_aria.Queue.Order.CurrentIndex > 0);
            _playerNextTrackAction.SetEnabled(_aria.Queue.Order.HasNext);
            _playerPlayPauseAction.SetEnabled(_aria.Queue.Length > 0);
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
            //var texture = await _resourceTextureLoader.LoadFromAlbumResourceAsync(coverInfo?.Id ?? Id.Empty, cancellationToken).ConfigureAwait(false);
            var texture = await Task.Run(
                () => _resourceTextureLoader.LoadFromAlbumResourceAsync(coverInfo?.Id ?? Id.Empty, cancellationToken),
                cancellationToken).ConfigureAwait(false);
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

    [LoggerMessage(LogLevel.Error, "Failed to enqueue tracks.")]
    static partial void LogFailedToEnqueueTracks(ILogger<PlayerPresenter> logger, Exception e);    
}