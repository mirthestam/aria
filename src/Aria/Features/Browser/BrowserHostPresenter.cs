using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Library;
using Aria.Features.Player.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using Aria.Infrastructure.Connection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser;

public partial class BrowserHostPresenter : IRootPresenter<BrowserHost>, IRecipient<LibraryUpdatedMessage>
{
    private readonly BrowserPagePresenter _browserPresenter;
    private readonly ILogger<BrowserHostPresenter> _logger;
    private readonly IAria _playerApi;

    public BrowserHostPresenter(ILogger<BrowserHostPresenter> logger,
        IMessenger messenger,
        IAria playerApi,
        BrowserPagePresenter browserPresenter)
    {
        _logger = logger;
        _playerApi = playerApi;
        _browserPresenter = browserPresenter;

        messenger.Register(this);
    }

    public BrowserHost View { get; set; } = null!;

    public void Attach(BrowserHost view, AttachContext context)
    {
        View = view;
        _browserPresenter.Attach(view.BrowserPage, context);
    }
    
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await DeterminePageAsync(cancellationToken);
        await _browserPresenter.RefreshAsync(cancellationToken);
    }    
    
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View.ToggleState(BrowserHost.BrowserState.EmptyCollection);
        }, cancellationToken).ConfigureAwait(false);        
        
        _browserPresenter.Reset();
    }
    
    public async void Receive(LibraryUpdatedMessage message)
    {
        try
        {
            await DeterminePageAsync();
        }
        catch 
        {
            // Ok
        }
    }
    
    private async Task DeterminePageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Update the page
            // TODO: Use a helper method for this
            var artists = await _playerApi.Library.GetArtistsAsync(cancellationToken);
            var artistsPresent = artists.Any();
            
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View.ToggleState(artistsPresent
                    ? BrowserHost.BrowserState.Browser
                    : BrowserHost.BrowserState.EmptyCollection);
            }, cancellationToken).ConfigureAwait(false);            
        }
        catch (OperationCanceledException)
        {
            // Ok   
        }
        catch (Exception e)
        {
            if (cancellationToken.IsCancellationRequested) return;
            
            LogCouldNotLoadLibrary(e);
            
            GLib.Functions.IdleAdd(0, () =>
            {
                View.ToggleState(BrowserHost.BrowserState.EmptyCollection);
                return false;
            });            
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to load your library")]
    partial void LogCouldNotLoadLibrary(Exception e);
}