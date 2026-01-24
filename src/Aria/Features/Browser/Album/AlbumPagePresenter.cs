using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser.Album;

public partial class AlbumPagePresenter(
    ILogger<AlbumPagePresenter> logger,
    IMessenger messenger,
    IAria aria,
    ResourceTextureLoader textureLoader)
{
    private AlbumInfo? _album;
    private AlbumPage? _view;
    private CancellationTokenSource? _loadCts;

    public void Attach(AlbumPage view)
    {
        _view = view;
        _view.PlayAlbumAction.OnActivate += PlayAlbumActionOnOnActivate;
        _view.EnqueueAlbumAction.OnActivate += EnqueueAlbumActionOnOnActivate;
        _view.ShowFullAlbumAction.OnActivate += ShowFullAlbumActionOnOnActivate;
    }

    private void ShowFullAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // If this is invoked, the album was shown partially.
        // Reload the album, but without any filters
        _view?.LoadAlbum(_album!);
    }

    public void Reset()
    {
        LogResetting(logger);
        try
        {
            AbortLoading();
        }
        catch (Exception e)
        {
            LogFailedToAbortLoading(logger, e);
        }
    }    

    private void AbortLoading()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
    }    
    
    private void EnqueueAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        LogEnqueueingAlbum(logger, _album?.Id ?? Id.Unknown);
        
        // TODO: Filtering is currently handled in the view.
        // This logic should be moved to the presenter
        // so the presenter explicitly controls which tracks are enqueued.
        // The same applies to the Play button.
    
        if (_album?.Id == null) return;
        _ = aria.Queue.EnqueueAlbum(_album);
        messenger.Send(new ShowToastMessage($"Album '{_album.Title}' added to queue."));
    }

    private void PlayAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        LogPlayingAlbum(logger, _album?.Id ?? Id.Unknown);
        
        if (_album?.Id == null) return;
        _ = aria.Queue.PlayAlbum(_album);
    }
    
    public async Task LoadAsync(AlbumInfo album, ArtistInfo? filteredArtist = null)
    {
        LogLoadingAlbum(logger, album.Id ?? Id.Unknown);
        
        // Always assume the album is out of date, or only partial.
        album = await aria.Library.GetAlbum(album.Id);
        
        AbortLoading();
        _loadCts = new CancellationTokenSource();
        var ct= _loadCts.Token;
            
        _album = album;

        try
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.LoadAlbum(album, filteredArtist);
                return false;
            });                        
            

            var assetId = album.Assets.FrontCover?.Id ?? Id.Empty;
            var texture = await textureLoader.LoadFromAlbumResourceAsync(assetId, ct);
            ct.ThrowIfCancellationRequested();
            
            if (texture == null)
            {
                LogCouldNotLoadAlbumCoverForAlbum(album.Id ?? Id.Empty);
                return;
            }

            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SetCover(texture);
                return false;
            });                        
        }
        catch (OperationCanceledException)
        {
        }        
        catch (Exception e)
        {
            LogCouldNotLoadAlbumCoverForAlbum(e, album.Id ?? Id.Empty);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Could not load album cover for album {albumId}")]
    partial void LogCouldNotLoadAlbumCoverForAlbum(Id albumId);

    [LoggerMessage(LogLevel.Warning, "Could not load album cover for album {albumId}")]
    partial void LogCouldNotLoadAlbumCoverForAlbum(Exception e, Id albumId);

    [LoggerMessage(LogLevel.Debug, "Resetting.")]
    static partial void LogResetting(ILogger<AlbumPagePresenter> logger);

    [LoggerMessage(LogLevel.Debug, "Enqueueing album {albumId} to queue.")]
    static partial void LogEnqueueingAlbum(ILogger<AlbumPagePresenter> logger, Id albumId);

    [LoggerMessage(LogLevel.Debug, "Playing album {albumId}.")]
    static partial void LogPlayingAlbum(ILogger<AlbumPagePresenter> logger, Id albumId);

    [LoggerMessage(LogLevel.Debug, "Loading album {albumId}")]
    static partial void LogLoadingAlbum(ILogger<AlbumPagePresenter> logger, Id albumId);

    [LoggerMessage(LogLevel.Error, "Failed to abort loading.")]
    static partial void LogFailedToAbortLoading(ILogger<AlbumPagePresenter> logger, Exception e);
}