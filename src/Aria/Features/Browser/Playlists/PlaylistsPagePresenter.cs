using Aria.Core;
using Aria.Core.Extraction;
using Aria.Features.Shell;
using Aria.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser.Playlists;

public class PlaylistsPagePresenter(ILogger<PlaylistsPagePresenter> logger, IAria aria) : IRootPresenter<PlaylistsPage>
{
    private readonly ILogger<PlaylistsPagePresenter> _logger = logger;

    private CancellationTokenSource? _loadCts;

    public void Attach(PlaylistsPage view, AttachContext context)
    {
        View = view;
        View.DeleteRequested += ViewOnDeleteRequested;
        View.TogglePage(PlaylistsPage.PlaylistsPages.Empty);
    }

    private async void ViewOnDeleteRequested(object? sender, Id e)
    {
        try
        {
            await aria.Library.DeletePlaylistAsync(e);
        }
        catch (Exception exception)
        {
            // TODO handle
        }
    }

    public PlaylistsPage? View { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
    }
    
    private void AbortAndClear()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        View?.ShowPlaylists([]);
    }    
    
    private async Task LoadAsync(CancellationToken externalCancellationToken = default)
    {

        AbortAndClear();
        
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        var cancellationToken = _loadCts.Token;
        
        try
        {
            var infos = await aria.Library.GetPlaylistsAsync(cancellationToken).ConfigureAwait(false);
            
            var models = infos.Select(PlaylistModel.NewForPlaylistInfo)
                .OrderBy(a => a.Playlist.Name)
                .ToList();
            cancellationToken.ThrowIfCancellationRequested();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                View?.ShowPlaylists(models);
            }, cancellationToken).ConfigureAwait(false);
            
            //LogAlbumsLoaded();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            //if (!cancellationToken.IsCancellationRequested) LogCouldNotLoadAlbums(e);
        }
    }    
    
    public async Task ResetAsync()
    {
        //LogResettingArtistPage();
        
        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View?.TogglePage(PlaylistsPage.PlaylistsPages.Empty);
        });        
    }    
}