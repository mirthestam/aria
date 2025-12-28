using Aria.Core;
using Aria.Core.Library;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Features.Browser;

public class BrowserPresenter : IRecipient<LibraryUpdatedMessage>, IRecipient<ConnectionChangedMessage>
{
    private readonly ArtistPagePresenter _artistPagePresenter;
    private readonly ArtistsPagePresenter _artistsPagePresenter;
    private readonly IPlaybackApi _playerApi;

    public BrowserPresenter(IPlaybackApi playerApi,
        IMessenger messenger,
        ArtistPagePresenter artistPagePresenter,
        ArtistsPagePresenter artistsPagePresenter)
    {
        _playerApi = playerApi;
        _artistPagePresenter = artistPagePresenter;
        _artistsPagePresenter = artistsPagePresenter;

        messenger.Register<LibraryUpdatedMessage>(this);
        messenger.Register<ConnectionChangedMessage>(this);
    }

    private Browser View { get; set; } = null!;

    public void Receive(ConnectionChangedMessage message)
    {
        if (message.Value == ConnectionState.Connected) _ = DeterminePageAsync();
    }

    public async void Receive(LibraryUpdatedMessage message)
    {
        try
        {
            // The library has been updated.
            // Update our library
            await DeterminePageAsync();
        }
        catch
        {
            // Eat
        }
    }

    public void Attach(Browser view)
    {
        View = view;
        _artistPagePresenter.Attach(view.ArtistPage);
        _artistsPagePresenter.Attach(view.ArtistsPage);
    }


    private async Task DeterminePageAsync()
    {
        // Update the page
        var artists = await _playerApi.Library.GetArtists();
        var artistsPresent = artists.Any();
        
        // TODO: Expose a method in the library for this functionality.  
        // For MPD, it can be implemented using the COUNT commands.
        View.TogglePage(artistsPresent ? Browser.BrowserPages.Browser : Browser.BrowserPages.EmptyCollection);
    }
}