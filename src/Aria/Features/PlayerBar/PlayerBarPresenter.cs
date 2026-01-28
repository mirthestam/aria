using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.PlayerBar;

public partial class PlayerBarPresenter : IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _aria;
    private readonly ILogger<PlayerBarPresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly ResourceTextureLoader _resourceTextureLoader;
    private PlayerBar? _view;

    private CancellationTokenSource? _coverArtCancellationTokenSource;

    public PlayerBarPresenter(IAria aria, IMessenger messenger, ILogger<PlayerBarPresenter> logger,
        ResourceTextureLoader resourceTextureLoader)
    {
        _logger = logger;
        _resourceTextureLoader = resourceTextureLoader;
        _aria = aria;
        _messenger = messenger;
        messenger.Register<PlayerStateChangedMessage>(this);
        messenger.Register<QueueStateChangedMessage>(this);
    }

    public void Attach(PlayerBar bar)
    {
        _view = bar;
        _view.EnqueueRequested += ViewOnEnqueueRequested;
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

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _ = RefreshCover(cancellationToken);

        Refresh(QueueStateChangedFlags.All);
        Refresh(PlayerStateChangedFlags.All);

        await Task.CompletedTask;
    }

    public void Reset()
    {
        AbortRefreshCover();

        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.ClearCover();
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
                _view?.SetPlaybackState(_aria.Player.State);
                return false;
            });
        }

        if (flags.HasFlag(PlayerStateChangedFlags.Progress))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SetProgress(_aria.Player.Progress.Elapsed, _aria.Player.Progress.Duration);
                return false;
            });
        }
    }

    private void Refresh(QueueStateChangedFlags flags)
    {
        if (!flags.HasFlag(QueueStateChangedFlags.Id) && !flags.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;
        
        if (_aria.Queue.Length == 0)
        {
            // The queue has changed and is empty.
            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SetCurrentTrack(null);
                return false;
            });
        }
        else
        {
            var track = _aria.Queue.CurrentTrack;

            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SetCurrentTrack(track?.Track);
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
                    _view?.ClearCover();
                    return false;
                });
                return;
            }

            var coverInfo = track.Track.Assets.FrontCover;
            var texture =
                await _resourceTextureLoader.LoadFromAlbumResourceAsync(coverInfo?.Id ?? Id.Empty, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            if (texture == null) return;

            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.LoadCover(texture);
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

    [LoggerMessage(LogLevel.Error, "Could not enqueue")]
    partial void LogCouldNotEnqueue(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to load album cover")]
    partial void LogFailedToLoadAlbumCover(Exception e);
}