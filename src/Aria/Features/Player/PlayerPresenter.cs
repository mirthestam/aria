using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Player.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using GLib;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;
using TimeSpan = System.TimeSpan;

namespace Aria.Features.Player;

public partial class PlayerPresenter : IRootPresenter<Player>,  IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _aria;
    private readonly IAriaControl _ariaControl;
    private readonly ILogger<PlayerPresenter> _logger;
    private readonly QueuePresenter _queuePresenter;
    private readonly ResourceTextureLoader _resourceTextureLoader;
    private readonly IMessenger _messenger;
    
    private CancellationTokenSource? _coverArtCancellationTokenSource;
    
    public Player? View { get; set; }
    
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
        
        InitializeActions(context);
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
        // Some changes implicitly effect playback order.
        var refreshPlaybackOrder = false;
        
        if (flags.HasFlag(QueueStateChangedFlags.Shuffle))
        {
            _ariaQueueShuffleAction.Enabled = _aria.Queue.Shuffle.Supported;
            _ariaQueueShuffleAction.SetState(Variant.NewBoolean(_aria.Queue.Shuffle.Enabled));
            refreshPlaybackOrder = true;
        }

        if (flags.HasFlag(QueueStateChangedFlags.Repeat))
        {
            _ariaQueueRepeatAction.Enabled = _aria.Queue.Repeat.Supported;
            _ariaQueueRepeatAction.SetState(Variant.NewString(_aria.Queue.Repeat.Mode.ToString()));
            refreshPlaybackOrder = true;
        }
        
        if (flags.HasFlag(QueueStateChangedFlags.Consume))
        {
            _ariaQueueConsumeAction.Enabled = _aria.Queue.Consume.Supported;
            _ariaQueueConsumeAction.SetState(Variant.NewBoolean(_aria.Queue.Consume.Enabled));
            refreshPlaybackOrder = true;
        }
        
        if (flags.HasFlag(QueueStateChangedFlags.Id) || flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                View?.SetPlaylistInfo(_aria.Queue.Order.CurrentIndex, _aria.Queue.Length);
                return false;
            });            
        }
        
        if (refreshPlaybackOrder || flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                _ariaPlayerPreviousTrackAction.SetEnabled(_aria.Queue.Order.CurrentIndex > 0);
                _ariaPlayerNextTrackAction.SetEnabled(_aria.Queue.Order.HasNext);
                _ariaPlayerPlayPauseAction.SetEnabled(_aria.Queue.Length > 0);
                return false;
            });
        }
        
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
    
    [LoggerMessage(LogLevel.Error, "Failed to load album cover")]
    partial void LogFailedToLoadAlbumCover(Exception e);
}