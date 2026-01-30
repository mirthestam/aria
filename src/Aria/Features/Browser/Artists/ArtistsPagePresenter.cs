using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Library;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser.Artists;

public partial class ArtistsPagePresenter : IRecipient<LibraryUpdatedMessage>
{
    private readonly ILogger<ArtistsPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IAria _aria;
    
    private const  ArtistsFilter DefaultFilter = ArtistsFilter.Artists;

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
            _view?.RefreshArtists([]);
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
        _view.ArtistSelected += id => { _messenger.Send(new ShowArtistDetailsMessage(id)); };
        _view.AllAlbumsRequested += () => { _messenger.Send(new ShowAllAlbumsMessage()); };
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
            var query = new ArtistQuery
            {
                RequiredRoles = ToRequiredRoles(_activeFilter),
                Sort = ArtistSort.ByName
            };
            
            var artists = await _aria.Library.GetArtistsAsync(query, cancellationToken);
            
            if (_view != null)
            {
                GLib.Functions.TimeoutAdd(0, 0, () =>
                {
                    if (cancellationToken.IsCancellationRequested) return false;
                    
                    _view.RefreshArtists(artists);
                    return false;
                });
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
}