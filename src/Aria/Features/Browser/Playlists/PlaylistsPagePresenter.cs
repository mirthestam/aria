using Aria.Core;
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
        View.TogglePage(PlaylistsPage.PlaylistsPages.Empty);
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
            var albums = await aria.Library.GetPlaylistsAsync(cancellationToken).ConfigureAwait(false);
            
            var albumModels = albums.Select(PlaylistModel.NewForPlaylistInfo)
                .OrderBy(a => a.Playlist.Name)
                .ToList();
            cancellationToken.ThrowIfCancellationRequested();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                View?.ShowPlaylists(albumModels);
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
    
    public void Reset()
    {
        //LogResettingArtistPage();
        
        GLib.Functions.IdleAdd(0, () =>
        {
            View?.TogglePage(PlaylistsPage.PlaylistsPages.Empty);
            return false;
        });        
    }    
}