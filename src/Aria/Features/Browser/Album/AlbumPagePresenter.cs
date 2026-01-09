using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser.Album;

public partial class AlbumPagePresenter
{
    private readonly ILogger<AlbumPagePresenter> _logger;
    private readonly ResourceTextureLoader _textureLoader;

    public AlbumPagePresenter(ILogger<AlbumPagePresenter> logger, ResourceTextureLoader textureLoader)
    {
        _logger = logger;
        _textureLoader = textureLoader;
    }

    public AlbumPage View { get; private set; }
    
    private AlbumInfo _album;

    public void Attach(AlbumPage view)
    {
        View = view;
    }

    public async Task LoadAsync(AlbumInfo album)
    {
        try
        {
            View.LoadAlbum(album);

            var texture =
                await _textureLoader.LoadFromAlbumResourceAsync(album.FrontCover != null
                    ? album.FrontCover.Id
                    : Id.Empty);
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