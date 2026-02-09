using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using GLib;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser.Artists;

public partial class ArtistsPagePresenter : IRecipient<LibraryUpdatedMessage>
{
    private readonly ILogger<ArtistsPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IAria _aria;
    
    private const  ArtistsFilter DefaultFilter = ArtistsFilter.Artists;
    private const ArtistNameDisplay DefaultDisplayName = ArtistNameDisplay.Name;

    private CancellationTokenSource? _refreshCancellationTokenSource;
    private ArtistsPage? _view;
    private ArtistsFilter _activeFilter = ArtistsFilter.Main;

    public ArtistsPagePresenter(ILogger<ArtistsPagePresenter> logger, IMessenger messenger, IAria aria)
    {
        _logger = logger;
        _messenger = messenger;
        _aria = aria;

        messenger.Register(this);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await RefreshArtistsAsync(cancellationToken);
    }

    public void Reset()
    {
        try
        {
            AbortRefresh();
            _view?.SetActiveFilter(_activeFilter);
            _view?.RefreshArtists([], DefaultDisplayName);
        }
        catch (Exception e)
        {
            LogFailedToResetArtistsPage(e);
        }
    }

    public void Receive(LibraryUpdatedMessage message)
    {
        _ = RefreshArtistsAsync();
    }

    public void Attach(ArtistsPage view)
    {
        _view = view;
        _view.ArtistSelected += artistInfo =>
        {
            var parameter = Variant.NewString(artistInfo.Id.ToString());
            _view.ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowArtist.Action}", parameter);
        };
        _view.SetActiveFilter(_activeFilter);
        
        _view.ArtistsFilterAction.OnChangeState += ArtistsFilterActionOnOnChangeState;
    }

    private void ArtistsFilterActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        var value = args.Value?.GetString(out _);
        _activeFilter = Enum.TryParse<ArtistsFilter>(value, out var parsed)
            ? parsed
            : DefaultFilter;
        _view?.SetActiveFilter(_activeFilter);
        
        _ = RefreshArtistsAsync();
    }

    private void AbortRefresh()
    {
        _refreshCancellationTokenSource?.Cancel();
        _refreshCancellationTokenSource?.Dispose();
        _refreshCancellationTokenSource = null;
    }

    private async Task RefreshArtistsAsync(CancellationToken externalCancellationToken = default)
    {
        LogRefreshingArtists();
        AbortRefresh();
        
        _refreshCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        var cancellationToken = _refreshCancellationTokenSource.Token;
        
        try
        {
            var (sort, displayField) = _activeFilter switch
            {
                // For now, only use this for the composer view.
                ArtistsFilter.Composers => (ArtistSort.ByNameSort, ArtistNameDisplay.NameSort),
                _ => (ArtistSort.ByName, ArtistNameDisplay.Name)
            };

            var query = new ArtistQuery
            {
                RequiredRoles = ToRequiredRoles(_activeFilter),
                Sort = sort
            };
            
            var artists = await _aria.Library.GetArtistsAsync(query, cancellationToken).ConfigureAwait(false);

            if (_view != null)
            {
                await GtkDispatch.InvokeIdleAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _view.RefreshArtists(artists, displayField);
                }, cancellationToken).ConfigureAwait(false);                        
            }

            LogArtistsRefreshed();
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LogCouldNotLoadArtists(e);
                _messenger.Send(new ShowToastMessage("Could not load artists"));
            }
        }
    }

    private static ArtistRoles? ToRequiredRoles(ArtistsFilter filter) =>
        filter switch
        {
            ArtistsFilter.Main => ArtistRoles.Main,
            ArtistsFilter.Artists => null,
            ArtistsFilter.Composers => ArtistRoles.Composer,
            ArtistsFilter.Conductors => ArtistRoles.Conductor,
            ArtistsFilter.Ensembles => ArtistRoles.Ensemble,
            ArtistsFilter.Performers => ArtistRoles.Performer,
            _ => null
        };
    
    [LoggerMessage(LogLevel.Error, "Could not load artists")]
    partial void LogCouldNotLoadArtists(Exception e);

    [LoggerMessage(LogLevel.Debug, "Refreshing artists")]
    partial void LogRefreshingArtists();

    [LoggerMessage(LogLevel.Debug, "Artists refreshed")]
    partial void LogArtistsRefreshed();

    [LoggerMessage(LogLevel.Error, "Failed to reset artists page")]
    partial void LogFailedToResetArtistsPage(Exception e);

    public async Task SelectArtist(Id artistId)
    {
        // This artist is must be selected in the sidebar.
        _view.SelectArtist(artistId);
    }
}