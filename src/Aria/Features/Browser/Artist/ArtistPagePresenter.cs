using System.ComponentModel;
using Aria.Core;
using Aria.Core.Library;
using Aria.Main;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser.Artist;

public partial class ArtistPagePresenter
{
    private readonly ILogger<ArtistPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IPlaybackApi _playbackApi;
    private readonly BrowserNavigationState _state;
    private readonly Infrastructure.ResourceTextureLoader _textureLoader;

    public ArtistPagePresenter(ILogger<ArtistPagePresenter> logger, IMessenger messenger, IPlaybackApi playbackApi,
        BrowserNavigationState state, Infrastructure.ResourceTextureLoader textureLoader)
    {
        _playbackApi = playbackApi;
        _messenger = messenger;
        _state = state;
        _logger = logger;
        _textureLoader = textureLoader;

        _state.PropertyChanged += StateOnPropertyChanged;
    }

    public ArtistPage View { get; private set; }

    private void StateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BrowserNavigationState.SelectedArtistId)) return;
        if (_state.SelectedArtistId != null) _ = LoadArtistAsync(_state.SelectedArtistId);
    }

    public void Attach(ArtistPage view)
    {
        View = view;
        View.TogglePage(ArtistPage.ArtistPages.Empty);
        View.AlbumSelected += (albumId, artistId) => _messenger.Send(new ShowAlbumDetailsMessage(albumId, artistId));
    }

    private async Task LoadArtistAsync(Id artistId)
    {
        try
        {
            // TODO: Looking up this artist here has a negative impact on performance.
            var artist = (await _playbackApi.Library.GetArtists()).First(a => a.Id == artistId);
            var albums = (await _playbackApi.Library.GetAlbums(artistId)).ToList();
            
            // These are albums where this artist actually is an album artist.
            // So we consider these 'their' albums.
            var discography = albums.Where(a => a.CreditsInfo.AlbumArtists.Any(ar => ar.Id == artistId)).ToList();

            // These are albums where this artist is not an album artist but appears on the album
            var appearsOn = albums.Except(discography);
            

            var albumModels = albums.Select(a => new AlbumModel(a)).ToList();
            
            
            View.TogglePage(albums.Count == 0 ? ArtistPage.ArtistPages.Empty : ArtistPage.ArtistPages.Artist);
            View.ShowArtist(artist, albumModels);

            foreach (var album in albumModels)
            {
                _ = LoadArtForModelAsync(album);
            }
        }
        catch (Exception e)
        {
            LogCouldNotLoadArtist(e, artistId);
            _messenger.Send(new ShowToastMessage("Could not load this artist"));
        }
    }
    
    private async Task LoadArtForModelAsync(AlbumModel model)
    {
        var artId = model.Album.Assets.FirstOrDefault(r => r.Type == AssetType.FrontCover)?.Id;
        if (artId == null) return;

        try
        {
            model.CoverTexture = await _textureLoader.LoadFromAlbumResourceAsync(artId);
        }
        catch(Exception e)
        {
            LogCouldNotLoadAlbumArtForAlbumid(e, model.Album.Id ?? Id.Empty);
        }
    }    

    [LoggerMessage(LogLevel.Error, "Could not load artist {artistId}")]
    partial void LogCouldNotLoadArtist(Exception e,Id artistId);

    [LoggerMessage(LogLevel.Warning, "Could not load album art for {albumId}")]
    partial void LogCouldNotLoadAlbumArtForAlbumid(Exception e, Id albumId);
}