using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gio;
using GObject;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser.Album;

public partial class AlbumPagePresenter
{
    private readonly ILogger<AlbumPagePresenter> _logger;
    private readonly ResourceTextureLoader _textureLoader;
    private readonly IPlaybackApi _playbackApi;
    
    public AlbumPagePresenter(ILogger<AlbumPagePresenter> logger, ResourceTextureLoader textureLoader, IPlaybackApi playbackApi)
    {
        _logger = logger;
        _textureLoader = textureLoader;
        _playbackApi = playbackApi;
    }

    public AlbumPage View { get; private set; }
    
    private AlbumInfo _album;

    public void Attach(AlbumPage view)
    {
        View = view;
        View.PlayAlbumAction.OnActivate += PlayAlbumActionOnOnActivate; 
        View.EnqueueAlbumAction.OnActivate += EnqueueAlbumActionOnOnActivate;
    }

    private void EnqueueAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        if (_album.Id == null) return;
        _ = _playbackApi.Queue.EnqueueAlbum(_album);
    }

    private void PlayAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        if (_album.Id == null) return;
        _ = _playbackApi.Queue.PlayAlbum(_album);
    }

    public async Task LoadAsync(AlbumInfo album)
    {
        _album = album;
        
        try
        {
            View.LoadAlbum(album);
            
            var assetId = album.Assets.FrontCover?.Id ?? Id.Empty;
            var texture = await _textureLoader.LoadFromAlbumResourceAsync(assetId);
            if (texture == null)
            {
                LogCouldNotLoadAlbumCoverForAlbum(album.Id ?? Id.Empty);
                return;
            }

            View.SetCover(texture);
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
}