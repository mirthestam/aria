using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Features.Browser.Album;
using Aria.Features.Details;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser;

public partial class BrowserPagePresenter
{
    // Actions
    private SimpleAction _searchAction;
    private SimpleAction _allAlbumsAction;
    private SimpleAction _showArtistAction;
    private SimpleAction _showAlbumAction;
    private SimpleAction _showAlbumForArtistAction;
    private SimpleAction _showPlaylistsAction;

    private SimpleAction _showTrackAction;
    
    private void InitializeActions(AttachContext context)
    {
        var browserActionGroup = SimpleActionGroup.New();

        browserActionGroup.AddAction(_searchAction = SimpleAction.New(AppActions.Browser.Search.Action, null));
        browserActionGroup.AddAction(_allAlbumsAction = SimpleAction.New(AppActions.Browser.AllAlbums.Action, null));
        browserActionGroup.AddAction(_showPlaylistsAction = SimpleAction.New(AppActions.Browser.Playlists.Action, null));        
        browserActionGroup.AddAction(_showArtistAction =
            SimpleAction.New(AppActions.Browser.ShowArtist.Action, GLib.VariantType.String));
        browserActionGroup.AddAction(_showAlbumAction =
            SimpleAction.New(AppActions.Browser.ShowAlbum.Action, GLib.VariantType.String));
        browserActionGroup.AddAction(_showAlbumForArtistAction =
            SimpleAction.New(AppActions.Browser.ShowAlbumForArtist.Action,
                GLib.VariantType.NewArray(GLib.VariantType.String)));
        browserActionGroup.AddAction(_showTrackAction =
            SimpleAction.New(AppActions.Browser.ShowTrack.Action, GLib.VariantType.String));
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.Search.Action}",
            [AppActions.Browser.Search.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.AllAlbums.Action}",
            [AppActions.Browser.AllAlbums.Accelerator]);
        context.SetAccelsForAction($"{AppActions.Browser.Key}.{AppActions.Browser.Playlists.Action}",
            [AppActions.Browser.Playlists.Accelerator]);        
        context.InsertAppActionGroup(AppActions.Browser.Key, browserActionGroup);

        _searchAction.OnActivate += SearchActionOnOnActivate;
        _allAlbumsAction.OnActivate += AllAlbumsActionOnOnActivate;
        _showArtistAction.OnActivate += ShowArtistActionOnOnActivate;
        _showAlbumAction.OnActivate += ShowAlbumActionOnOnActivate;
        _showAlbumForArtistAction.OnActivate += ShowAlbumForArtistActionOnOnActivate;
        _showTrackAction.OnActivate += ShowTrackActionOnOnActivate;
        _showPlaylistsAction.OnActivate += ShowPlaylistsActionOnOnActivate;
    }

    private async void ShowTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }

            var parameter = args.Parameter.GetString(out _);
            var trackId = _ariaControl.Parse(parameter);

            var trackInfo = (AlbumTrackInfo?)await _aria.Library.GetItemAsync(trackId);

            if (trackInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this track."));
                return;
            }
            
            var albumInfo = await _aria.Library.GetAlbumAsync(trackInfo.Track.AlbumId);
            if (albumInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this album."));
                return;
            }
            
            var trackDetailsPresenter = _presenterFactory.Create<TrackDetailsDialogPresenter>();

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                var dialog = TrackDetailsDialog.NewWithProperties([]);
                trackDetailsPresenter.Attach(dialog);
                trackDetailsPresenter.Load(trackInfo, albumInfo);
                dialog.Present(View);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async void ShowAlbumForArtistActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }

            var parameters = args.Parameter.GetStrv(out _);

            var albumId = _ariaControl.Parse(parameters[0]);
            var artistId = _ariaControl.Parse(parameters[1]);

            var albumInfo = await _aria.Library.GetAlbumAsync(albumId);
            if (albumInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this album."));
                return;
            }

            var artistInfo = await _aria.Library.GetArtistAsync(artistId);
            if (artistInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this artist."));
                return;
            }

            _albumPagePresenter = _presenterFactory.Create<AlbumPagePresenter>();

            GLib.Functions.IdleAdd(0, () =>
            {
                var albumPageView = View?.PushAlbumPage();
                if (albumPageView == null) return false;

                _albumPagePresenter.Attach(albumPageView);

                _ = _albumPagePresenter.LoadAsync(albumInfo, artistInfo);
                return false;
            });
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
    }

    private async void ShowAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }

            var serializedId = args.Parameter.GetString(out _);
            var albumId = _ariaControl.Parse(serializedId);

            var albumInfo = await _aria.Library.GetAlbumAsync(albumId);
            if (albumInfo == null)
            {
                _messenger.Send(new ShowToastMessage("Could not find this album."));
                return;
            }

            _albumPagePresenter = _presenterFactory.Create<AlbumPagePresenter>();

            GLib.Functions.IdleAdd(0, () =>
            {
                var albumPageView = View?.PushAlbumPage();
                if (albumPageView == null) return false;
                _albumPagePresenter.Attach(albumPageView);

                _ = _albumPagePresenter.LoadAsync(albumInfo);
                return false;
            });
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
    }

    private async void ShowArtistActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null)
            {
                return;
            }

            var serializedId = args.Parameter.GetString(out _);
            var artistId = _ariaControl.Parse(serializedId);
            LogShowingArtistDetailsForArtist(artistId);

            await _artistPagePresenter.LoadArtistAsync(artistId);

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.ShowArtistDetailRoot();
            });
        }
        catch (Exception e)
        {
            LogFailedToParseArtistId(e);
        }
    }

    private void AllAlbumsActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        ShowAllAlbums();
    }

    private void ShowPlaylistsActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        ShowPlaylists();
    }    
    
    private void SearchActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // User wants to start the search functionality
        View?.StartSearch();
    }

    [LoggerMessage(LogLevel.Error, "Failed to parse artist id")]
    partial void LogFailedToParseArtistId(Exception e);

    [LoggerMessage(LogLevel.Debug, "Showing artist details for artist {artistId}")]
    partial void LogShowingArtistDetailsForArtist(Id artistId);
}