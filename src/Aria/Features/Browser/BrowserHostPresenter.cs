using System.Runtime.CompilerServices;
using Aria.Core;
using Aria.Core.Library;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser;

public partial class BrowserHostPresenter : IRecipient<LibraryUpdatedMessage>, IRecipient<ConnectionChangedMessage>
{
    private readonly ILogger<BrowserHostPresenter> _logger;
    private readonly IPlaybackApi _playerApi;
    
    private readonly BrowserPagePresenter _browserPresenter;

    public BrowserHostPresenter(IPlaybackApi playerApi,
        ILogger<BrowserHostPresenter> logger,
        IMessenger messenger,
        BrowserPagePresenter browserPresenter)
    {
        _logger = logger;
        _playerApi = playerApi;
        _browserPresenter = browserPresenter;

        messenger.Register<LibraryUpdatedMessage>(this);
        messenger.Register<ConnectionChangedMessage>(this);
    }

    private BrowserHost View { get; set; } = null!;

    public void Receive(ConnectionChangedMessage message)
    {
        if (message.Value == ConnectionState.Connected) _ = DeterminePageAsync();
    }

    public void Receive(LibraryUpdatedMessage message)
    {
        _ =  DeterminePageAsync();
    }

    public void Attach(BrowserHost view)
    {
        View = view;
        _browserPresenter.Attach(view.BrowserPage);
    }
    
    private async Task DeterminePageAsync()
    {
        try
        {
            // Update the page
            var artists = await _playerApi.Library.GetArtists();
            var artistsPresent = artists.Any();
        
            // TODO: Expose a method in the library for this functionality.  
            // For MPD, it can be implemented using the COUNT commands.
            View.ToggleState(artistsPresent ? BrowserHost.BrowserState.Browser : BrowserHost.BrowserState.EmptyCollection);
        }
        catch (Exception e)
        {
            LogCouldNotLoadLibrary(e);
            View.ToggleState(BrowserHost.BrowserState.EmptyCollection);
        }
    }
    
    [LoggerMessage(LogLevel.Error, "Failed to load your library")]
    partial void LogCouldNotLoadLibrary(Exception e);    
}