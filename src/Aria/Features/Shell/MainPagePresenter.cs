using System.Xml.Xsl;
using Aria.Core.Connection;
using Aria.Features.Browser;
using Aria.Features.Player;
using Aria.Features.PlayerBar;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Shell;

public partial class MainPagePresenter : IRecipient<ConnectionStateChangedMessage>
{
    private readonly ILogger<MainPagePresenter> _logger;
    private readonly BrowserHostPresenter _browserHostPresenter;
    private readonly PlayerPresenter _playerPresenter;
    private readonly PlayerBarPresenter _playerBarPresenter;

    private CancellationTokenSource? _connectionCancellationTokenSource;
    
    private Task _activeTask = Task.CompletedTask;

    public MainPagePresenter(ILogger<MainPagePresenter> logger,
        BrowserHostPresenter browserHostPresenter,
        PlayerPresenter playerPresenter,
        PlayerBarPresenter playerBarPresenter,
        IMessenger messenger)
    {
        _logger = logger;
        _browserHostPresenter = browserHostPresenter;
        _playerPresenter = playerPresenter;
        _playerBarPresenter = playerBarPresenter;
        messenger.Register(this);
    }

    public void Attach(MainPage view)
    {
        _browserHostPresenter.Attach(view.BrowserHost);
        _playerBarPresenter.Attach(view.PlayerBar);
        _playerPresenter.Attach(view.Player);
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
            
            //await _playerPresenter.RefreshAsync(cancellationToken);
            //await _playerBarPresenter.RefreshAsync();
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
}