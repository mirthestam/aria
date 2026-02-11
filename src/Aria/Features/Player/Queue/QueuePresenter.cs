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

namespace Aria.Features.Player.Queue;

public partial class QueuePresenter : IRecipient<QueueStateChangedMessage>, IRecipient<PlayerStateChangedMessage>
{
    private readonly ILogger<QueuePresenter> _logger;
    private readonly ResourceTextureLoader _resourceTextureLoader;
    private readonly IMessenger _messenger;
    private readonly IAria _aria;

    private Queue? _view;

    private CancellationTokenSource? _loadCts;

    // Reuse UI models between refreshes to avoid churn when only order changes.
    private readonly Dictionary<Id, QueueTrackModel> _modelsByQueueTrackId = new();

    private void Abort()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
    }
    
    private async Task AbortAndClearAsync()
    {
        Abort();
        
        await GtkDispatch.InvokeIdleAsync(() => 
        {
            _view?.TogglePage(Queue.QueuePages.Empty);
            _view?.RefreshTracks([]);
        });        
    }        

    public QueuePresenter(IMessenger messenger, IAria aria, ILogger<QueuePresenter> logger, ResourceTextureLoader resourceTextureLoader)
    {
        _logger = logger;
        _resourceTextureLoader = resourceTextureLoader;
        _messenger = messenger;
        _aria = aria;
        _messenger.Register<QueueStateChangedMessage>(this);
        _messenger.Register<PlayerStateChangedMessage>(this);
    }

    public void Attach(Queue view)
    {
        _view = view;
        _view.TrackSelectionChanged += ViewOnTrackSelectionChanged;
        _view.EnqueueRequested += ViewOnEnqueueRequested;
        _view.MoveRequested += ViewOnMoveRequested;
        _view.TogglePage(Queue.QueuePages.Empty);
    }
    
    private async void ViewOnMoveRequested(object? sender, MoveRequestedEventArgs args)
    {
        try
        {
            await _aria.Queue.MoveAsync(args.SourceId, args.TargetIndex);
        }
        catch (Exception exception)
        {
            _messenger.Send(new ShowToastMessage("Could not move"));
            LogCouldNotMove(exception);
        }
    }

    private async void ViewOnEnqueueRequested(object? sender, EnqueueRequestedEventArgs args)
    {
        try
        {
            var info = await _aria.Library.GetItemAsync(args.Id);
            if (info == null) return;
            
            await _aria.Queue.EnqueueAsync(info, args.Index);
        }
        catch (Exception exception)
        {
            _messenger.Send(new ShowToastMessage("Could not enqueue"));
            LogCouldNotEnqueue(exception);
        }
    }

    public async Task RefreshAsync(CancellationToken externalCancellationToken = default)
    {
        await RefreshTracksAsync(externalCancellationToken);
    }

    public async Task ResetAsync()
    {
        await AbortAndClearAsync();
    }    
    
    public void Receive(PlayerStateChangedMessage message)
    {
        if (!message.Value.HasFlag(PlayerStateChangedFlags.PlaybackState)) return;
        
        if (_aria.Player.State == PlaybackState.Stopped)
        {
            // TODO: Deselect any track.s
            //but this might already be part of currentTrack check.
        }
    }

    public async void Receive(QueueStateChangedMessage message)
    {
        try
        {
            if (message.Value.HasFlag(QueueStateChangedFlags.Id))
                // The identifier of the playlist has changed.
                // We need to reload the tracks
                await RefreshTracksAsync();

            if (message.Value.HasFlag(QueueStateChangedFlags.PlaybackOrder))
            {
                await GtkDispatch.InvokeIdleAsync(() =>
                {
                    _view?.SelectTrackIndex(_aria.Queue.Order.CurrentIndex);
                });        
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not update queue");
        }

    }

    private void ViewOnTrackSelectionChanged(object? sender, TrackSelectionChangedEventArgs e)
    {
        _aria.Player.PlayAsync((int)e.SelectedIndex);
    }

    private async Task RefreshTracksAsync(CancellationToken externalCancellationToken = default)
    {
        try
        {
            Abort();
            LogRefreshingPlaylist();

            _loadCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var ct = _loadCts.Token;

            var tracks = await _aria.Queue.GetTracksAsync().ConfigureAwait(false);

            // Build ordered model list by reusing existing models where possible.
            var newOrderedModels = new List<QueueTrackModel>();
            var seenIds = new HashSet<Id>();

            foreach (var t in tracks)
            {
                ct.ThrowIfCancellationRequested();

                var queueTrackId = t.Id;
                if (queueTrackId == null)
                {
                    newOrderedModels.Add(QueueTrackModel.NewFromQueueTrackInfo(t));
                    continue;
                }

                seenIds.Add(queueTrackId);

                if (!_modelsByQueueTrackId.TryGetValue(queueTrackId, out var model))
                {
                    model = QueueTrackModel.NewFromQueueTrackInfo(t);
                    _modelsByQueueTrackId[queueTrackId] = model;
                }
                else
                {
                    model.Position = t.Position;
                }

                newOrderedModels.Add(model);
            }

            // Purge removed tracks from cache
            var removedIds = _modelsByQueueTrackId.Keys.Where(id => !seenIds.Contains(id)).ToList();
            foreach (var removedId in removedIds)
            {
                _modelsByQueueTrackId.Remove(removedId);
            }

            ct.ThrowIfCancellationRequested();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                _view?.RefreshTracks(newOrderedModels);
                _view?.TogglePage(_aria.Queue.Length != 0 ? Queue.QueuePages.Tracks : Queue.QueuePages.Empty);
            }, ct).ConfigureAwait(false);

            await ProcessArtworkAsync(newOrderedModels, ct);
        }
        catch (OperationCanceledException)
        {
            // Ok
        }
        catch (Exception e)
        {
            LogCouldNotLoadTracks(e);
            _view?.TogglePage(Queue.QueuePages.Empty);
            _messenger.Send(new ShowToastMessage("Could not load playlist"));
        }
    }
    
    private async Task ProcessArtworkAsync(IEnumerable<QueueTrackModel> models, CancellationToken ct)
    {
        _logger.LogDebug("Loading album artwork.");
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 1,
            CancellationToken = ct
        };

        try
        {
            await Parallel.ForEachAsync(models, options,
                async (model, token) =>
                {
                    ct.ThrowIfCancellationRequested();
                    if (model.CoverTexture != null) return;
                    await LoadArtForModelAsync(model, token);
                });
            
            _logger.LogInformation("Artwork loading completed.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Artwork loading aborted.");
        }
    }

    private async Task LoadArtForModelAsync(QueueTrackModel model, CancellationToken ct = default)
    {
        var album = await _aria.Library.GetAlbumAsync(model.AlbumId, ct).ConfigureAwait(false);
        if (album == null) return;
        
        var artId = album.Assets.FirstOrDefault(r => r.Type == AssetType.FrontCover)?.Id;
        if (artId == null) return;
        
        try
        {
            model.CoverTexture = await _resourceTextureLoader.LoadFromAlbumResourceAsync(artId, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ok
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not load artwork for {Track}", model.TrackId);
        }
    }

    [LoggerMessage(LogLevel.Error, "Could not load tracks")]
    partial void LogCouldNotLoadTracks(Exception e);

    [LoggerMessage(LogLevel.Information, "Refreshing playlist")]
    partial void LogRefreshingPlaylist();

    [LoggerMessage(LogLevel.Error, "Could not enqueue")]
    partial void LogCouldNotEnqueue(Exception e);
    
    [LoggerMessage(LogLevel.Error, "Could not move")]
    partial void LogCouldNotMove(Exception e);    
}