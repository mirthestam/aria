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
    IPresenter<BrowserPage>,
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
    private SimpleAction _allAlbumsAction;
    private SimpleAction _showArtistAction;

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

    public BrowserPage? View { get; private set; }

    public void Attach(BrowserPage view, AttachContext context)
    {
        View = view;
        _artistPagePresenter.Attach(view.LibraryArtistPage);
        _artistsPagePresenter.Attach(view.LibraryArtistsPage);
        _albumsPagePresenter.Attach(view.LibraryAlbumsPage);
        _searchPagePresenter.Attach(view.SearchPage);

        
        var browserActionGroup = SimpleActionGroup.New();
        
        browserActionGroup.AddAction(_searchAction = SimpleAction.New(Accelerators.Browser.Search.Name, null));
        browserActionGroup.AddAction(_allAlbumsAction = SimpleAction.New(Accelerators.Browser.AllAlbums.Name, null));
        browserActionGroup.AddAction(_showArtistAction = SimpleAction.New(Accelerators.Browser.ShowArtist.Name,  GLib.VariantType.String));
        context.SetAccelsForAction($"{Accelerators.Browser.Key}.{Accelerators.Browser.Search.Name}", [Accelerators.Browser.Search.Accels]);
        context.SetAccelsForAction($"{Accelerators.Browser.Key}.{Accelerators.Browser.AllAlbums.Name}", [Accelerators.Browser.AllAlbums.Accels]);        
        context.InsertAppActionGroup(Accelerators.Browser.Key, browserActionGroup);
        
        _searchAction.OnActivate += SearchActionOnOnActivate;
        _allAlbumsAction.OnActivate += AllAlbumsActionOnOnActivate;
        _showArtistAction.OnActivate += ShowArtistActionOnOnActivate;
        
        // TODO: queue
                
     
        View.EnqueueDefaultAction.OnActivate += DefaultEnqueueActionOnOnActivate;
        View.EnqueueEndAction.OnActivate += EnqueueEndActionOnOnActivate;
        View.EnqueueNextAction.OnActivate += EnqueueNextActionOnOnActivate;
        View.EnqueueReplaceAction.OnActivate += PlayActionOnOnActivate;
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
        
            var artistInfo = await _aria.Library.GetArtistAsync(artistId);
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

    private void AllAlbumsActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        ShowAllAlbums();
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
            View?.ShowAllAlbumsRoot();
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
            View?.ShowArtistDetailRoot();
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
            var albumPageView = View?.PushAlbumPage();
            if (albumPageView == null) return false;
            _albumPagePresenter.Attach(albumPageView);

            // TODO: CancellationToken ?
            _ = _albumPagePresenter.LoadAsync(message.Album, message.Artist);            
            return false;
        });        
    }

    public void Receive(ShowAllAlbumsMessage message)
    {
        ShowAllAlbums();
    }

    private void ShowAllAlbums()
    {
        LogShowingAllAlbums();
        _browserNavigationState.SelectedArtistId = null;

        GLib.Functions.IdleAdd(0, () =>
        {
            View?.ShowAllAlbumsRoot();
            return false;
        });    
    }
    
    public void Receive(ShowArtistDetailsMessage message)
    {
        LogShowingArtistDetailsForArtist(message.Artist.Id ?? Id.Empty);
        
        GLib.Functions.IdleAdd(0, () =>
        {
            View?.ShowArtistDetailRoot();
            return false;
        });        
        
        _browserNavigationState.SelectedArtistId = message.Artist.Id;
    }

    private void SearchActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // User wants to start the search functionality
        View?.StartSearch();
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
    
    [LoggerMessage(LogLevel.Error, "Failed to parse artist id")]
    partial void LogFailedToParseArtistId(Exception e);    
}