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

public partial class PlayerPresenter : IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _api;
    private readonly ILogger<PlayerPresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly PlaylistPresenter _playlistPresenter;
    private readonly ResourceTextureLoader _resourceTextureLoader;
    
    private CancellationTokenSource? _coverArtCancellationTokenSource;
    
    private Player? _view;

    public PlayerPresenter(ILogger<PlayerPresenter> logger, IMessenger messenger, IAria api,
        ResourceTextureLoader resourceTextureLoader, PlaylistPresenter playlistPresenter)
    {
        _logger = logger;
        _resourceTextureLoader = resourceTextureLoader;
        _playlistPresenter = playlistPresenter;
        _api = api;
        _messenger = messenger;
        messenger.RegisterAll(this);
    }
    
    public void Attach(Player player)
    {
        _view = player;

        _playlistPresenter.Attach(_view.Playlist);

        _view.NextAction.OnActivate += NextActionOnOnActivate;
        _view.PrevAction.OnActivate += PrevActionOnOnActivate;
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
        _view?.ClearCover();        
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
        _view?.PlayerStateChanged(flags, _api);
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
        // This allows cover loading to be cancelled both internally (e.g., when a new song starts)
        // and externally (e.g., when the component cancels connection via the cancellation token passed to ConnectAsync).
        // The linked token ensures that cancelling either source will cancel the cover loading operation.
        _coverArtCancellationTokenSource = externalCancellationToken != CancellationToken.None 
            ? CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken) 
            : new CancellationTokenSource();
            
        var cancellationToken = _coverArtCancellationTokenSource.Token;            
        
        try
        {
            var song = _api.QueueProxy.CurrentSong;
            if (song == null) return;

            var coverInfo = song.Assets.FrontCover;
            var texture = await _resourceTextureLoader.LoadFromAlbumResourceAsync(coverInfo?.Id ?? Id.Empty, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            if (texture == null) return;
            _view?.LoadCover(texture);
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

    private void Refresh(QueueStateChangedFlags flags)
    {
        _view?.QueueStateChanged(flags, _api);
        if (!flags.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;

        _view?.PrevAction.SetEnabled(_api.QueueProxy.Order.CurrentIndex > 0);
        _view?.NextAction.SetEnabled(_api.QueueProxy.Order.HasNext);
        _ = LoadCover();
    }
    
    private async void PrevActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _api.PlayerProxy.PreviousAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to previous song"));
        }
    }

    private async void NextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _api.PlayerProxy.NextAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to next song"));
        }
    }

    [LoggerMessage(LogLevel.Error, "Player action failed: {action}")]
    partial void PlayerActionFailed(Exception e, string? action);

    [LoggerMessage(LogLevel.Error, "Failed to load album cover")]
    partial void LogFailedToLoadAlbumCover(Exception e);

    
}