using System.ComponentModel;
using Aria.Core;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Features.Browser.Artist;

public class ArtistPagePresenter
{
    private readonly IMessenger _messenger;
    private readonly IPlaybackApi _playbackApi;
    private readonly BrowserNavigationState _state;

    public ArtistPagePresenter(IPlaybackApi playbackApi, IMessenger messenger, BrowserNavigationState state)
    {
        _playbackApi = playbackApi;
        _messenger = messenger;
        _state = state;

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
    }

    private async Task LoadArtistAsync(Id artistId)
    {
        var albums = (await _playbackApi.Library.GetAlbums(artistId)).ToList();

        View.TogglePage(albums.Count == 0 ? ArtistPage.ArtistPages.Empty : ArtistPage.ArtistPages.Artist);
        View.ShowAlbums(albums, artistId);
    }
}