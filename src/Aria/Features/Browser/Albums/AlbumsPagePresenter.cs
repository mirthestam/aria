using Aria.Core;
using Aria.Core.Library;
using Aria.Features.Browser.Artist;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using ResourceTextureLoader = Aria.Infrastructure.ResourceTextureLoader;

namespace Aria.Features.Browser.Albums;

public partial class AlbumsPagePresenter :   IRecipient<LibraryUpdatedMessage>,
    IRecipient<ConnectionChangedMessage>
{
    private readonly ILogger<AlbumsPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IPlaybackApi _playbackApi;
    private readonly ResourceTextureLoader _textureLoader;

    public AlbumsPagePresenter(ILogger<AlbumsPagePresenter> logger, IMessenger messenger, IPlaybackApi playbackApi,
        ResourceTextureLoader textureLoader)
    {
        _playbackApi = playbackApi;
        _messenger = messenger;
        _logger = logger;
        _textureLoader = textureLoader;

        _messenger.Register<LibraryUpdatedMessage>(this);
        _messenger.Register<ConnectionChangedMessage>(this);
    }

    public AlbumsPage View { get; private set; }
    
    public void Attach(AlbumsPage view)
    {
        View = view;
        View.AlbumSelected += (albumId, artistId) => _messenger.Send(new ShowAlbumDetailsMessage(albumId, artistId));
    }

    private async Task LoadAsync()
    {
        // TODO: This generates a large amount of traffic during application startup.
        // Prefer loading artwork only for albums that are currently in view.

        try
        {
            var albums = (await _playbackApi.Library.GetAlbums()).ToList();
            var albumModels = albums.Select(a => new AlbumsAlbumModel(a)).ToList();
            View.ShowAlbums(albumModels);

            foreach (var album in albumModels)
            {
                _ = LoadArtForModelAsync(album);
            }
        }
        catch (Exception e)
        {
            LogCouldNotLoadAlbums(e);
        }
    }
    
    private async Task LoadArtForModelAsync(AlbumsAlbumModel model)
    {
        // TODO: I currently load artwork for all albums.
        // However, it may be better to start loading artwork only when an album enters the UI view.
        var artId = model.Album.Resources.FirstOrDefault(r => r.Type == ResourceType.FrontCover)?.Id;
        if (artId == null) return;

        try
        {
            model.CoverTexture = await _textureLoader.LoadFromAlbumResourceAsync(artId);
        }
        catch(Exception e)
        {
            LogResourceResourceidNotFoundInLibrary(e, artId);
        }
    }    


    [LoggerMessage(LogLevel.Error, "Could not load albums")]
    partial void LogCouldNotLoadAlbums(Exception e);

    public void Receive(LibraryUpdatedMessage message)
    {
        _ = LoadAsync();
    }

    public void Receive(ConnectionChangedMessage message)
    {
        if (message.Value == ConnectionState.Connected)
        {
            _ = LoadAsync();            
        }
    }

    [LoggerMessage(LogLevel.Warning, "Resource {resourceId} not found in library")]
    partial void LogResourceResourceidNotFoundInLibrary(Exception e, Id resourceId);
}