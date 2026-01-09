using Aria.Core;
using Aria.Core.Library;
using Aria.Features.Browser.Album;
using Aria.Features.Browser.Albums;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Search;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser;

public partial class BrowserPagePresenter : 
    IRecipient<LibraryUpdatedMessage>,
    IRecipient<ConnectionChangedMessage>,
    IRecipient<ShowArtistDetailsMessage>,
    IRecipient<ShowAllAlbumsMessage>,
    IRecipient<ShowAlbumDetailsMessage>
{
    private readonly ILogger<BrowserPage> _logger;
    private readonly IPlaybackApi _playerApi;
    
    private readonly ArtistPagePresenter _artistPagePresenter;
    private readonly ArtistsPagePresenter _artistsPagePresenter;
    private readonly SearchPagePresenter _searchPagePresenter;
    private readonly AlbumsPagePresenter _albumsPagePresenter;
    
    private readonly IAlbumPagePresenterFactory _albumPagePresenterFactory;

    private readonly BrowserNavigationState _browserNavigationState;
    
    private BrowserPage View { get; set; } = null!;
    
    private SimpleAction _searchAction;
    
    public BrowserPagePresenter(ILogger<BrowserPage> logger,
        IMessenger messenger,
        IPlaybackApi playerApi,
        BrowserNavigationState browserNavigationState,
        AlbumsPagePresenter albumsPagePresenter,
        ArtistPagePresenter artistPagePresenter,
        ArtistsPagePresenter artistsPagePresenter,
        IAlbumPagePresenterFactory albumPagePresenterFactory,
        SearchPagePresenter searchPagePresenter)
    {
        _logger = logger;
        _playerApi = playerApi;
        _artistPagePresenter = artistPagePresenter;
        _artistsPagePresenter = artistsPagePresenter;
        _searchPagePresenter = searchPagePresenter;
        _albumsPagePresenter = albumsPagePresenter;
        _browserNavigationState = browserNavigationState;
        _albumPagePresenterFactory = albumPagePresenterFactory;

        messenger.Register<LibraryUpdatedMessage>(this);
        messenger.Register<ConnectionChangedMessage>(this);
        messenger.Register<ShowArtistDetailsMessage>(this);
        messenger.Register<ShowAllAlbumsMessage>(this);
        messenger.Register<ShowAlbumDetailsMessage>(this);
    }
    
    public void Attach(BrowserPage view)
    {
        View = view;
        _artistPagePresenter.Attach(view.LibraryArtistPage);
        _artistsPagePresenter.Attach(view.LibraryArtistsPage);
        _albumsPagePresenter.Attach(view.LibraryAlbumsPage);        
        _searchPagePresenter.Attach(view.SearchPage);
        
        var actionGroup = SimpleActionGroup.New();
        
        _searchAction = SimpleAction.New("search", null);
        _searchAction.OnActivate += SearchActionOnOnActivate;
        actionGroup.AddAction(_searchAction);
        
        view.InsertActionGroup("browser", actionGroup);        
    }

    private void SearchActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // User wants to start the search functionality
        View.StartSearch();
    }

    private async Task UpdateLibrary()
    {
        try
        {
            var artists = await _playerApi.Library.GetArtists();
            View.LibraryArtistsPage.RefreshArtists(artists);
        }
        catch (Exception e)
        {
            LogCouldNotLoadLibrary(e);
        }
    }

    public void Receive(LibraryUpdatedMessage message)
    {
        _ = UpdateLibrary();
    }

    public void Receive(ConnectionChangedMessage message)
    {

    }
    
    public void Receive(ShowArtistDetailsMessage message)
    {
        View.ShowArtistDetailRoot();
        _browserNavigationState.SelectedArtistId = message.Artist.Id;
    }    
    
    public void Receive(ShowAlbumDetailsMessage message)
    {
        if (message.Artist != null)
        {
            _browserNavigationState.SelectedArtistId = message.Artist.Id;
        }
        
        var presenter = _albumPagePresenterFactory.Create();
        var albumPageView = View.PushAlbumPage();
        presenter.Attach(albumPageView);
        
        _ = presenter.LoadAsync(message.Album);
    }    
    
    public void Receive(ShowAllAlbumsMessage message)
    {
        _browserNavigationState.SelectedArtistId = null;
        View.ShowAllAlbumsRoot();
    }    
    
    [LoggerMessage(LogLevel.Error, "Failed to load your library")]
    partial void LogCouldNotLoadLibrary(Exception e);
}