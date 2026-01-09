using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure;
using Aria.Main;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using GLib;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser.Artists;

public partial class ArtistsPagePresenter : IRecipient<LibraryUpdatedMessage>, IRecipient<ConnectionChangedMessage>
{
    private readonly ILogger<ArtistsPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly BrowserNavigationState _navigationState;
    private readonly IPlaybackApi _playbackApi;
    private ArtistsPage? _view;

    

    public ArtistsPagePresenter(BrowserNavigationState navigationState, IMessenger messenger, IPlaybackApi playbackApi,ILogger<ArtistsPagePresenter> logger)
    {
        _logger = logger;
        _navigationState = navigationState;
        _messenger = messenger;
        _playbackApi = playbackApi;

        messenger.Register<LibraryUpdatedMessage>(this);
        messenger.Register<ConnectionChangedMessage>(this);
    }

    public void Receive(ConnectionChangedMessage message)
    {
        if (message.Value == ConnectionState.Connected) _ = RefreshArtistsAsync();
    }

    public void Receive(LibraryUpdatedMessage message)
    {
        _ = RefreshArtistsAsync();
    }

    public void Attach(ArtistsPage view)
    {
        _view = view;
        _view.ArtistSelected += id =>
        {
            _messenger.Send(new ShowArtistDetailsMessage(id));
        };
        _view.AllAlbumsRequested += () =>
        {
            _messenger.Send(new ShowAllAlbumsMessage());
        };
    }

    private async Task RefreshArtistsAsync()
    {
        try
        {
            var artists = await _playbackApi.Library.GetArtists();
            artists = artists.Where(a => a.Roles.HasFlag(ArtistRoles.Composer));
            // TODO: Remove this filter here.We need something clever on the UI
            _view?.RefreshArtists(artists);
        }
        catch (Exception e)
        {
            LogCouldNotLoadArtists(e);
            _messenger.Send(new ShowToastMessage("Could not load artists"));
        }
    }
    
    [LoggerMessage(LogLevel.Error, "Could not load artists")]
    partial void LogCouldNotLoadArtists(Exception e);    
}