using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Player.Playlist;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Player;

public partial class PlayerPresenter : IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _aria;
    private readonly ILogger<PlayerPresenter> _logger;
    private readonly PlaylistPresenter _playlistPresenter;
    private readonly ResourceTextureLoader _resourceTextureLoader;
    
    private CancellationTokenSource? _coverArtCancellationTokenSource;
    
    private Player? _view;

    public PlayerPresenter(ILogger<PlayerPresenter> logger, IMessenger messenger, IAria aria,
        ResourceTextureLoader resourceTextureLoader, PlaylistPresenter playlistPresenter)
    {
        _logger = logger;
        _resourceTextureLoader = resourceTextureLoader;
        _playlistPresenter = playlistPresenter;
        _aria = aria;
        messenger.RegisterAll(this);
    }
    
    public void Attach(Player player)
    {
        _view = player;
        _view.SeekRequested += ViewOnSeekRequested;

        _playlistPresenter.Attach(_view.Playlist);
        
    }

    private async Task ViewOnSeekRequested(TimeSpan position, CancellationToken cancellationToken)
    {
        await _aria.Player.SeekAsync(position, cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _ = LoadCover(cancellationToken);
        
        Refresh(QueueStateChangedFlags.All);
        Refresh(PlayerStateChangedFlags.All);

        await _playlistPresenter.RefreshAsync();

    }

    public void Reset()
    {
        _playlistPresenter.Reset();
        AbortLoadCover();
        
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
        if (flags.HasFlag(QueueStateChangedFlags.Id) || flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SetPlaylistInfo(_aria.Queue.Order.CurrentIndex, _aria.Queue.Length);
                return false;
            });            
        }
        
        if (!flags.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;
        _ = LoadCover();
    }
    
    private void AbortLoadCover()
    {
        _coverArtCancellationTokenSource?.Cancel();
        _coverArtCancellationTokenSource?.Dispose();
        _coverArtCancellationTokenSource = null;
    }

    private async Task LoadCover(CancellationToken externalCancellationToken = default)
    {
        AbortLoadCover();
        
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
            if (track == null) return;

            var coverInfo = track.Assets.FrontCover;
            var texture = await _resourceTextureLoader.LoadFromAlbumResourceAsync(coverInfo?.Id ?? Id.Empty, cancellationToken);
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
    
    [LoggerMessage(LogLevel.Error, "Failed to load album cover")]
    partial void LogFailedToLoadAlbumCover(Exception e);
}