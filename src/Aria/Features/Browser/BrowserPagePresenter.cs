using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Features.Browser.Album;
using Aria.Features.Browser.Albums;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Search;
using Aria.Features.Player.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser;

public partial class BrowserPagePresenter : IPresenter<BrowserPage>
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
    private SimpleAction _showAlbumAction;
    private SimpleAction _showAlbumForArtistAction;
    
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
        
        browserActionGroup.AddAction(_searchAction = SimpleAction.New(AppActions.Browser.Search.Action, null));
        browserActionGroup.AddAction(_allAlbumsAction = SimpleAction.New(AppActions.Browser.AllAlbums.Action, null));
        browserActionGroup.AddAction(_showArtistAction = SimpleAction.New(AppActions.Browser.ShowArtist.Action,  GLib.VariantType.String));
        browserActionGroup.AddAction(_showAlbumAction = SimpleAction.New(AppActions.Browser.ShowAlbum.Action,  GLib.VariantType.String));
        browserActionGroup.AddAction(_showAlbumForArtistAction = SimpleAction.New(AppActions.Browser.ShowAlbumForArtist.Action, GLib.VariantType.NewArray(GLib.VariantType.String)));
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.Search.Action}", [AppActions.Browser.Search.Accelerator!]);
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.AllAlbums.Action}", [AppActions.Browser.AllAlbums.Accelerator!]);        
        context.InsertAppActionGroup(AppActions.Browser.Key, browserActionGroup);
        
        _searchAction.OnActivate += SearchActionOnOnActivate;
        _allAlbumsAction.OnActivate += AllAlbumsActionOnOnActivate;
        _showArtistAction.OnActivate += ShowArtistActionOnOnActivate;
        _showAlbumAction.OnActivate += ShowAlbumActionOnOnActivate;
        _showAlbumForArtistAction.OnActivate += ShowAlbumForArtistActionOnOnActivate;
        

    }

    private async void ShowAlbumForArtistActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }
            
            var parameters = args.Parameter.GetStrv(out _);
            
            var albumId = _ariaControl.Parse(parameters[0]);
            var artistId = _ariaControl.Parse(parameters[1]);
        
            var albumInfo = await _aria.Library.GetAlbumAsync(albumId);
            if (albumInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this album."));
                return;
            }
            
            var artistInfo = await _aria.Library.GetArtistAsync(artistId);
            if (artistInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this artist."));
                return;
            }
            
            _albumPagePresenter = _albumPagePresenterFactory.Create();
        
            GLib.Functions.IdleAdd(0, () =>
            {
                var albumPageView = View?.PushAlbumPage();
                if (albumPageView == null) return false;
                _albumPagePresenter.Attach(albumPageView);
                
                _ = _albumPagePresenter.LoadAsync(albumInfo, artistInfo);            
                return false;
            });
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
    }

    private async void ShowAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }
            
            var serializedId = args.Parameter.GetString(out _);
            var albumId = _ariaControl.Parse(serializedId);
        
            var albumInfo = await _aria.Library.GetAlbumAsync(albumId);
            if (albumInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this album."));
                return;
            }
        
            _albumPagePresenter = _albumPagePresenterFactory.Create();
        
            GLib.Functions.IdleAdd(0, () =>
            {
                var albumPageView = View?.PushAlbumPage();
                if (albumPageView == null) return false;
                _albumPagePresenter.Attach(albumPageView);
                
                _ = _albumPagePresenter.LoadAsync(albumInfo);            
                return false;
            });
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
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
        
            LogShowingArtistDetailsForArtist(artistId);
        
            GLib.Functions.IdleAdd(0, () =>
            {
                View?.ShowArtistDetailRoot();
                return false;
            });        
        
            _browserNavigationState.SelectedArtistId = artistId;
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

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        LogRefreshingBrowserPage();

        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View?.ShowAllAlbumsRoot();
        }, cancellationToken).ConfigureAwait(false);        
        
        // Preload the artists
        await _artistsPagePresenter.RefreshAsync(cancellationToken);
        
        // Load all albums 
        await _albumsPagePresenter.RefreshAsync(cancellationToken).ContinueWith(t =>
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
    
    [LoggerMessage(LogLevel.Error, "Failed to parse artist id")]
    partial void LogFailedToParseArtistId(Exception e);    
}