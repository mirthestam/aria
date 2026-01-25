using System.Xml.Xsl;
using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Browser;
using Aria.Features.Player;
using Aria.Features.PlayerBar;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Shell;

public partial class MainPagePresenter : IRecipient<ConnectionStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly ILogger<MainPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IAria _aria;
    private readonly IAriaControl _ariaControl;
    private readonly BrowserHostPresenter _browserHostPresenter;
    private readonly PlayerPresenter _playerPresenter;
    private readonly PlayerBarPresenter _playerBarPresenter;
    
    private CancellationTokenSource? _connectionCancellationTokenSource;
    
    private Task _activeTask = Task.CompletedTask;
    
    private MainPage? _view;

    public MainPagePresenter(ILogger<MainPagePresenter> logger,
        BrowserHostPresenter browserHostPresenter,
        PlayerPresenter playerPresenter,
        PlayerBarPresenter playerBarPresenter,
        IMessenger messenger, IAria aria, IAriaControl ariaControl)
    {
        _ariaControl = ariaControl;
        _aria = aria;
        _logger = logger;
        _messenger = messenger;
        _browserHostPresenter = browserHostPresenter;
        _playerPresenter = playerPresenter;
        _playerBarPresenter = playerBarPresenter;
        messenger.RegisterAll(this);
    }

    public void Attach(MainPage view)
    {
        _view = view;
        _browserHostPresenter.Attach(view.BrowserHost);
        _playerBarPresenter.Attach(view.PlayerBar);
        _playerPresenter.Attach(view.Player);
        
        view.NextAction.OnActivate += NextActionOnOnActivate;
        view.PrevAction.OnActivate += PrevActionOnOnActivate;        
        view.PlayPauseAction.OnActivate += PlayPauseActionOnOnActivate;
        view.ShowArtistAction.OnActivate += ShowArtistActionOnOnActivate;
    }

    private async void ShowArtistActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }
            
            var serializedId = args.Parameter.GetString(out _);
            var artistId = _ariaControl.Parse(serializedId);
        
            var artistInfo = await _aria.Library.GetArtist(artistId);
            if (artistInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this artist."));
                return;
            }
        
            _messenger.Send(new ShowArtistDetailsMessage(artistInfo));
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
    }

    public void Receive(ConnectionStateChangedMessage message)
    {
        switch (message.Value)
        {
            case ConnectionState.Connected:
                CancelActiveRefresh();
                _activeTask = SequenceTaskAsync(() => OnConnectedAsync());
                break;

            case ConnectionState.Disconnected:
                CancelActiveRefresh();
                _activeTask = SequenceTaskAsync(() => OnDisconnectedAsync());
                break;
            case ConnectionState.Connecting:
                _ = OnConnectingAsync();
                break;
            default:
                throw new InvalidOperationException("Unexpected connection state");
        }
    }
    
    private void CancelActiveRefresh()
    {
        if (_connectionCancellationTokenSource == null) return;
        _connectionCancellationTokenSource.Cancel();
        _connectionCancellationTokenSource.Dispose();
        _connectionCancellationTokenSource = null;
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogLoadingUiForConnectedBackend();
            Refresh(QueueStateChangedFlags.All);
            
            await _playerPresenter.RefreshAsync(cancellationToken);
            await _playerBarPresenter.RefreshAsync();
            await _browserHostPresenter.RefreshAsync(cancellationToken);

            _logger.LogInformation(cancellationToken.IsCancellationRequested
                ? "UI refresh was cancelled before completion."
                : "UI succesfully refreshed.");
        }
        catch (OperationCanceledException)
        {
            LogUiRefreshAborted();
        }
        finally
        {
            if (_connectionCancellationTokenSource?.Token == cancellationToken)
            {
                _connectionCancellationTokenSource.Dispose();
                _connectionCancellationTokenSource = null;
            }
        }    
    }

    private async Task Reset(CancellationToken cancellationToken = default)
    {
        try
        {
            LogDisconnectedFromBackendUnloadingUi();
            _browserHostPresenter.Reset();
            _playerPresenter.Reset();
            _playerBarPresenter.Reset();
            LogUiIsReset();
            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            LogFailedToResetUi(e);
        }        
    }
    
    private async Task OnConnectedAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAsync(cancellationToken);
    }
    
    private async Task OnConnectingAsync()
    {
        await Task.CompletedTask;
    }

    private async Task OnDisconnectedAsync(CancellationToken cancellationToken = default)
    {
        await Reset(cancellationToken);
    }

    private async Task SequenceTaskAsync(Func<Task> action)
    {
        try
        {
            await _activeTask;
        }
        catch(Exception e)
        {
            LogSequenceTaskAbortedDueToException(e);
        }

        await action();
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
                    await _aria.Player.PlayAsync();
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
    
    public void Receive(QueueStateChangedMessage message)
    {
        Refresh(message.Value);
    }
    
    private void Refresh(QueueStateChangedFlags flags)
    {
        if (!flags.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;

        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.PrevAction.SetEnabled(_aria.Queue.Order.CurrentIndex > 0);
            _view?.NextAction.SetEnabled(_aria.Queue.Order.HasNext);
            _view?.PlayPauseAction.SetEnabled(_aria.Queue.Length > 0);
            return false;
        });        
    }    
    
    [LoggerMessage(LogLevel.Error, "Player action failed: {action}")]
    partial void PlayerActionFailed(Exception e, string? action);    
    
    [LoggerMessage(LogLevel.Information, "Backend connected - Connecting UI.")]
    partial void LogLoadingUiForConnectedBackend();

    [LoggerMessage(LogLevel.Information, "backend disconnected - Disconnecting UI.")]
    partial void LogDisconnectedFromBackendUnloadingUi();

    [LoggerMessage(LogLevel.Warning, "New connection established. Aborting current UI refresh.")]
    partial void LogNewConnectionEstablishedAbortingCurrentUiRefresh();

    [LoggerMessage(LogLevel.Information, "Connection lost during UI refresh. Aborting UI refresh.")]
    partial void LogConnectionLostDuringUiRefreshAbortingUiRefresh();

    [LoggerMessage(LogLevel.Information, "UI refresh aborted.")]
    partial void LogUiRefreshAborted();

    [LoggerMessage(LogLevel.Information, "UI is reset.")]
    partial void LogUiIsReset();

    [LoggerMessage(LogLevel.Error, "Failed to reset UI.")]
    partial void LogFailedToResetUi(Exception e);

    [LoggerMessage(LogLevel.Warning, "Sequence task aborted due to exception.")]
    partial void LogSequenceTaskAbortedDueToException(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to parse artist id")]
    partial void LogFailedToParseArtistId(Exception e);
}