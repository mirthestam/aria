using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using ResourceTextureLoader = Aria.Infrastructure.ResourceTextureLoader;

namespace Aria.Features.Browser.Albums;

public partial class AlbumsPagePresenter : IRecipient<LibraryUpdatedMessage>
{
    private readonly ILogger<AlbumsPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IAria _aria;
    private readonly ResourceTextureLoader _textureLoader;

    private CancellationTokenSource? _loadCts;

    public AlbumsPagePresenter(ILogger<AlbumsPagePresenter> logger, IMessenger messenger, IAria aria,
        ResourceTextureLoader textureLoader)
    {
        _aria = aria;
        _messenger = messenger;
        _logger = logger;
        _textureLoader = textureLoader;

        _messenger.RegisterAll(this);
    }

    private AlbumsPage? _view;

    public void Attach(AlbumsPage view)
    {
        _view = view;
        _view.AlbumSelected += (albumId, artistId) => _messenger.Send(new ShowAlbumDetailsMessage(albumId, artistId));
    }
    
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
    }

    public void Reset()
    {
        LogResetting();
        AbortAndClear();
    }    
    
    private void AbortAndClear()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        _view?.ShowAlbums([]);
    }

    public void Receive(LibraryUpdatedMessage message)
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync(CancellationToken externalCancellationToken = default)
    {
        LogLoadingAlbums();
        AbortAndClear();
        
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        var cancellationToken = _loadCts.Token;


        try
        {
            var albums = await _aria.Library.GetAlbums(cancellationToken);
            var albumModels = albums.Select(a => new AlbumsAlbumModel(a)).ToList();
            cancellationToken.ThrowIfCancellationRequested();

            GLib.Functions.TimeoutAdd(0, 0, () =>
            {
                if (cancellationToken.IsCancellationRequested) return false;
                    
                _view?.ShowAlbums(albumModels);
                return false;
            });            
            
            
            
            LogAlbumsLoaded();

            // This performs slow I/O (loading all album covers).
            // Ideally, I/O should not block this thread, but it does, so it is wrapped in a Task.
            // We do not await it; album art is fetched in the background.
            _ = Task.Run(() => ProcessArtworkAsync(albumModels, cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested) LogCouldNotLoadAlbums(e);
        }
    }

    private async Task ProcessArtworkAsync(IEnumerable<AlbumsAlbumModel> models, CancellationToken ct)
    {
        LogLoadingAlbumsArtwork();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4,
            CancellationToken = ct
        };

        try
        {
            await Parallel.ForEachAsync(models, options,
                async (model, token) =>
                {
                    ct.ThrowIfCancellationRequested();
                    await LoadArtForModelAsync(model, token);
                });
            
            LogAlbumsArtworkLoaded();
        }
        catch (OperationCanceledException)
        {
            LogArtworkLoadingAborted();
        }
    }

    private async Task LoadArtForModelAsync(AlbumsAlbumModel model, CancellationToken ct = default)
    {
        var artId = model.Album.Assets.FirstOrDefault(r => r.Type == AssetType.FrontCover)?.Id;
        if (artId == null) return;

        try
        {
            model.CoverTexture = await _textureLoader.LoadFromAlbumResourceAsync(artId, ct);
        }
        catch (OperationCanceledException)
        {
            // Ok
        }
        catch (Exception e)
        {
            LogResourceResourceIdNotFoundInLibrary(e, artId);
        }
    }

    [LoggerMessage(LogLevel.Error, "Could not load albums")]
    partial void LogCouldNotLoadAlbums(Exception e);

    [LoggerMessage(LogLevel.Warning, "Resource {resourceId} not found in library")]
    partial void LogResourceResourceIdNotFoundInLibrary(Exception e, Id resourceId);

    [LoggerMessage(LogLevel.Debug, "Resetting albums page")]
    partial void LogResetting();

    [LoggerMessage(LogLevel.Debug, "Loading albums")]
    partial void LogLoadingAlbums();

    [LoggerMessage(LogLevel.Debug, "Albums loaded.")]
    partial void LogAlbumsLoaded();

    [LoggerMessage(LogLevel.Debug, "Loading albums artwork")]
    partial void LogLoadingAlbumsArtwork();

    [LoggerMessage(LogLevel.Debug, "Albums artwork loaded.")]
    partial void LogAlbumsArtworkLoaded();

    [LoggerMessage(LogLevel.Debug, "Artwork loading aborted.")]
    partial void LogArtworkLoadingAborted();
}