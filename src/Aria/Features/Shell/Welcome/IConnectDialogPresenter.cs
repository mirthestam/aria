using Aria.Core.Connection;

namespace Aria.Features.Shell.Welcome;

public interface IConnectDialogPresenter
{
    bool CanHandle(IConnectionProfile profile);
    Task<ConnectDialogResult> ShowAsync(Gtk.Widget parent, IConnectionProfile profile, CancellationToken ct = default);
}