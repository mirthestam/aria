using System.ComponentModel;
using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser.Artist;

public partial class ArtistPagePresenter
{
    private readonly ILogger<ArtistPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IAria _aria;
    private readonly BrowserNavigationState _state;
    private readonly ResourceTextureLoader _textureLoader;
    
    private CancellationTokenSource? _loadArtistCancellationTokenSource;

    public ArtistPagePresenter(ILogger<ArtistPagePresenter> logger, IMessenger messenger, IAria aria,
        BrowserNavigationState state, ResourceTextureLoader textureLoader)
    {
        _aria = aria;
        _messenger = messenger;
        _state = state;
        _logger = logger;
        _textureLoader = textureLoader;

        _state.PropertyChanged += StateOnPropertyChanged;
    }

    private ArtistPage? _view;

    public void Attach(ArtistPage view)
    {
        _view = view;
        _view.TogglePage(ArtistPage.ArtistPages.Empty);
        _view.AlbumSelected += (albumId, artistId) => _messenger.Send(new ShowAlbumDetailsMessage(albumId, artistId));
    }    
    
    public void Reset()
    {
        LogResettingArtistPage();
        
        _view?.TogglePage(ArtistPage.ArtistPages.Empty);
        _view?.SetTitle("Artist"); // TODO now this name is defined in 2 places
    }    
    
    private void StateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BrowserNavigationState.SelectedArtistId)) return;
        if (_state.SelectedArtistId == null) return;
        
        LogArtistSelectionChanged(_state.SelectedArtistId);
        
        _loadArtistCancellationTokenSource?.Cancel();
        _loadArtistCancellationTokenSource?.Dispose();
        _loadArtistCancellationTokenSource = new CancellationTokenSource();
        _ = LoadArtistAsync(_state.SelectedArtistId, _loadArtistCancellationTokenSource.Token);
    }
    
    private async Task LoadArtistAsync(Id artistId, CancellationToken ct)
    {
        LogLoadingArtist(artistId);
        try
        {
            var artist = await _aria.Library.GetArtist(artistId, ct);
            if (artist == null) throw new InvalidOperationException("Artist not found");

            var albums = (await _aria.Library.GetAlbums(artistId, ct)).ToList();
            var albumModels = albums.Select(a => new AlbumModel(a)).ToList();

            _view?.TogglePage(albums.Count == 0 ? ArtistPage.ArtistPages.Empty : ArtistPage.ArtistPages.Artist);
            _view?.ShowArtist(artist, albumModels);

            LogArtistLoadedLoadingArtwork(artistId);
            
            foreach (var album in albumModels) _ = LoadArtForModelAsync(album, ct);
            
            LogArtistArtworkLoaded(artistId);
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception e)
        {
            LogCouldNotLoadArtist(e, artistId);
            _messenger.Send(new ShowToastMessage("Could not load this artist"));
        }
    }

    private async Task LoadArtForModelAsync(AlbumModel model, CancellationToken ct)
    {
        var artId = model.Album.Assets.FirstOrDefault(r => r.Type == AssetType.FrontCover)?.Id;
        if (artId == null) return;

        try
        {
            model.CoverTexture = await _textureLoader.LoadFromAlbumResourceAsync(artId, ct);
        }
        catch (Exception e)
        {
            LogCouldNotLoadAlbumArtForAlbumId(e, model.Album.Id ?? Id.Empty);
        }
    }

    [LoggerMessage(LogLevel.Error, "Could not load artist {artistId}")]
    partial void LogCouldNotLoadArtist(Exception e, Id artistId);

    [LoggerMessage(LogLevel.Warning, "Could not load album art for {albumId}")]
    partial void LogCouldNotLoadAlbumArtForAlbumId(Exception e, Id albumId);


    [LoggerMessage(LogLevel.Debug, "Resetting artist page")]
    partial void LogResettingArtistPage();

    [LoggerMessage(LogLevel.Debug, "Artist selection changed: {artistId}")]
    partial void LogArtistSelectionChanged(Id artistId);

    [LoggerMessage(LogLevel.Debug, "Loading artist {artistId}")]
    partial void LogLoadingArtist(Id artistId);

    [LoggerMessage(LogLevel.Debug, "Artist {artistId} loaded. Loading artwork.")]
    partial void LogArtistLoadedLoadingArtwork(Id artistId);

    [LoggerMessage(LogLevel.Debug, "Artist {artistId} artwork loaded.")]
    partial void LogArtistArtworkLoaded(Id artistId);
}