using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Features.Browser.Artists;

public class ArtistsPagePresenter : IRecipient<LibraryUpdatedMessage>, IRecipient<ConnectionChangedMessage>
{
    private readonly IMessenger _messenger;
    private readonly BrowserNavigationState _navigationState;
    private readonly IPlaybackApi _playbackApi;
    private ArtistsPage? _view;

    public ArtistsPagePresenter(BrowserNavigationState navigationState, IMessenger messenger, IPlaybackApi playbackApi)
    {
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
        _view.ArtistSelected += id => { _navigationState.SelectedArtistId = id; };
    }

    private async Task RefreshArtistsAsync()
    {
        var artists = await _playbackApi.Library.GetArtists();
        artists = artists.Where(a => a.Roles.HasFlag(ArtistRoles.Conductor)); // hehe
        _view?.RefreshArtists(artists);
    }
}