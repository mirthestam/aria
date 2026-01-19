using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Library;
using Aria.Infrastructure;
using Aria.Infrastructure.Connection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser;

public partial class BrowserHostPresenter : IRecipient<LibraryUpdatedMessage>
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

    private BrowserHost View { get; set; } = null!;

    public void Attach(BrowserHost view)
    {
        View = view;
        _browserPresenter.Attach(view.BrowserPage);
    }
    
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await DeterminePageAsync(cancellationToken);
        await _browserPresenter.RefreshAsync(cancellationToken);
    }    
    
    public void Reset()
    {
        View.ToggleState(BrowserHost.BrowserState.EmptyCollection);
        _browserPresenter.Reset();
    }
    
    public void Receive(LibraryUpdatedMessage message)
    {
        _ = DeterminePageAsync();
    }
    
    private async Task DeterminePageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Update the page
            var artists = await _playerApi.Library.GetArtists(cancellationToken);
            var artistsPresent = artists.Any();

            // TODO: Expose a method in the library for this functionality.  
            // For MPD, it can be implemented using the COUNT commands.
            View.ToggleState(artistsPresent
                ? BrowserHost.BrowserState.Browser
                : BrowserHost.BrowserState.EmptyCollection);
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception e)
        {
            if (cancellationToken.IsCancellationRequested) return;
            
            LogCouldNotLoadLibrary(e);
            View.ToggleState(BrowserHost.BrowserState.EmptyCollection);
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to load your library")]
    partial void LogCouldNotLoadLibrary(Exception e);
}