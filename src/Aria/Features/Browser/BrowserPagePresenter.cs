using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Features.Browser.Album;
using Aria.Features.Browser.Albums;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Search;
using Aria.Features.Shell;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser;

public partial class BrowserPagePresenter :
    IRecipient<ShowArtistDetailsMessage>,
    IRecipient<ShowAllAlbumsMessage>,
    IRecipient<ShowAlbumDetailsMessage>
{
    private readonly IAria _aria;
    private readonly IAriaControl _ariaControl;
    private readonly IMessenger _messenger;
    private readonly IAlbumPagePresenterFactory _albumPagePresenterFactory;
    private readonly AlbumsPagePresenter _albumsPagePresenter;

    private readonly ArtistPagePresenter _artistPagePresenter;
    private readonly ArtistsPagePresenter _artistsPagePresenter;

    private readonly BrowserNavigationState _browserNavigationState;
    private readonly ILogger<BrowserPage> _logger;
    private readonly SearchPagePresenter _searchPagePresenter;

    private AlbumPagePresenter? _albumPagePresenter;

    private SimpleAction _searchAction;

    public BrowserPagePresenter(ILogger<BrowserPage> logger,
        IMessenger messenger,
        IAria aria,
        IAriaControl ariaControl,
        BrowserNavigationState browserNavigationState,
        AlbumsPagePresenter albumsPagePresenter,
        ArtistPagePresenter artistPagePresenter,
        ArtistsPagePresenter artistsPagePresenter,
        IAlbumPagePresenterFactory albumPagePresenterFactory,
        SearchPagePresenter searchPagePresenter)
    {
        _logger = logger;
        _messenger = messenger;
        _aria = aria;
        _ariaControl = ariaControl;
        _artistPagePresenter = artistPagePresenter;
        _artistsPagePresenter = artistsPagePresenter;
        _searchPagePresenter = searchPagePresenter;
        _albumsPagePresenter = albumsPagePresenter;
        _browserNavigationState = browserNavigationState;
        _albumPagePresenterFactory = albumPagePresenterFactory;

        messenger.Register<ShowArtistDetailsMessage>(this);
        messenger.Register<ShowAllAlbumsMessage>(this);
        messenger.Register<ShowAlbumDetailsMessage>(this);
    }

    private BrowserPage? _view;

    public void Attach(BrowserPage view)
    {
        _view = view;
        _artistPagePresenter.Attach(view.LibraryArtistPage);
        _artistsPagePresenter.Attach(view.LibraryArtistsPage);
        _albumsPagePresenter.Attach(view.LibraryAlbumsPage);
        _searchPagePresenter.Attach(view.SearchPage);

        var browserActionGroup = SimpleActionGroup.New();

        _searchAction = SimpleAction.New("search", null);
        _searchAction.OnActivate += SearchActionOnOnActivate;
        browserActionGroup.AddAction(_searchAction);

        view.InsertActionGroup("browser", browserActionGroup);
     
        _view.EnqueueDefaultAction.OnActivate += DefaultEnqueueActionOnOnActivate;
        _view.EnqueueEndAction.OnActivate += EnqueueEndActionOnOnActivate;
        _view.EnqueueNextAction.OnActivate += EnqueueNextActionOnOnActivate;
        _view.EnqueueReplaceAction.OnActivate += PlayActionOnOnActivate;
    }
    
    private void DefaultEnqueueActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(IQueue.DefaultEnqueueAction, args);
    private void EnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.EnqueueEnd, args);
    private void EnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.EnqueueNext, args);
    private void PlayActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => EnqueueHandler(EnqueueAction.Replace, args);

    private async void EnqueueHandler(EnqueueAction enqueueAction, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var serializedIds = args.Parameter!.GetStrv(out _);
            var ids = serializedIds.Select(_ariaControl.Parse).ToArray();
            
            // Enqueue the items by id
            await EnqueueIds(enqueueAction, ids).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToEnqueueTracks(_logger, e);
            _messenger.Send(new ShowToastMessage($"Failed to enqueue tracks."));
        }        
    }
    
    private async Task EnqueueIds(EnqueueAction action, Id[] ids)
    {
        var items = new List<Info>();
        foreach (var id in ids)
        {
            // Would be great to have 'GetItems' instead of foreach here.
            var item =await _aria.Library.GetItemAsync(id).ConfigureAwait(false);
            if (item == null) continue;
            items.Add(item);
        }

        await _aria.Queue.EnqueueAsync(items, action).ConfigureAwait(false);
        
        switch (action)
        {
            case EnqueueAction.Replace:
                _messenger.Send(new ShowToastMessage($"Playing tracks."));
                break;
            case EnqueueAction.EnqueueNext:
                _messenger.Send(new ShowToastMessage($"Playing tracks Next."));
                break;
            case EnqueueAction.EnqueueEnd:
                _messenger.Send(new ShowToastMessage($"Added tracks to end of queue."));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }    

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        LogRefreshingBrowserPage();

        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.ShowAllAlbumsRoot();
            return false;
        });
        
        // Preload the artists
        await _artistsPagePresenter.RefreshAsync(cancellationToken);
        
        // Load all albums in the background
        _ = _albumsPagePresenter.RefreshAsync(cancellationToken).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            if (t.Exception is not null) LogFailedToLoadLibrary(t.Exception);
        }, TaskScheduler.Default);

        if (cancellationToken.IsCancellationRequested)
            LogBrowserPageRefreshCancelled();
        else
            LogBrowserPageRefreshed();
    }

    public void Reset()
    {
        try
        {
            LogResettingBrowserPage();
            _albumPagePresenter?.Reset();

            _albumsPagePresenter.Reset();
            _artistsPagePresenter.Reset();
            _artistPagePresenter.Reset();
            _searchPagePresenter.Reset();
            _view?.ShowArtistDetailRoot();
            LogBrowserPageReset();
        }
        catch (Exception e)
        {
            LogFailedToResetBrowserPage(e);
        }
    }

    public void Receive(ShowAlbumDetailsMessage message)
    {
        LogShowingAlbumDetailsForAlbum(message.Album.Id);
        
        _albumPagePresenter = _albumPagePresenterFactory.Create();
        
        GLib.Functions.IdleAdd(0, () =>
        {
            var albumPageView = _view?.PushAlbumPage();
            if (albumPageView == null) return false;
            _albumPagePresenter.Attach(albumPageView);

            // TODO: CancellationToken ?
            _ = _albumPagePresenter.LoadAsync(message.Album, message.Artist);            
            return false;
        });        
    }

    public void Receive(ShowAllAlbumsMessage message)
    {
        LogShowingAllAlbums();
        _browserNavigationState.SelectedArtistId = null;

        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.ShowAllAlbumsRoot();
            return false;
        });        
    }
    
    public void Receive(ShowArtistDetailsMessage message)
    {
        LogShowingArtistDetailsForArtist(message.Artist.Id ?? Id.Empty);
        
        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.ShowArtistDetailRoot();
            return false;
        });        
        
        _browserNavigationState.SelectedArtistId = message.Artist.Id;
    }

    private void SearchActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // User wants to start the search functionality
        _view?.StartSearch();
    }
    
    [LoggerMessage(LogLevel.Debug, "Refreshing browser page...")]
    partial void LogRefreshingBrowserPage();    

    [LoggerMessage(LogLevel.Error, "Failed to load your library")]
    partial void LogCouldNotLoadLibrary(Exception e);

    [LoggerMessage(LogLevel.Debug, "Browser page refresh cancelled.")]
    partial void LogBrowserPageRefreshCancelled();

    [LoggerMessage(LogLevel.Information, "Browser page refreshed.")]
    partial void LogBrowserPageRefreshed();

    [LoggerMessage(LogLevel.Debug, "Resetting browser page...")]
    partial void LogResettingBrowserPage();

    [LoggerMessage(LogLevel.Debug, "Browser page reset.")]
    partial void LogBrowserPageReset();

    [LoggerMessage(LogLevel.Debug, "Showing album details for {albumId}")]
    partial void LogShowingAlbumDetailsForAlbum(Id albumId);

    [LoggerMessage(LogLevel.Debug, "Showing all albums")]
    partial void LogShowingAllAlbums();

    [LoggerMessage(LogLevel.Debug, "Showing artist details for artist {artistId}")]
    partial void LogShowingArtistDetailsForArtist(Id artistId);

    [LoggerMessage(LogLevel.Error, "Failed to reset browser page")]
    partial void LogFailedToResetBrowserPage(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to load library")]
    partial void LogFailedToLoadLibrary(Exception e);
    
    [LoggerMessage(LogLevel.Error, "Failed to enqueue tracks.")]
    static partial void LogFailedToEnqueueTracks(ILogger<BrowserPage> logger, Exception e);    
}