using Aria.Infrastructure;
using Gtk;

namespace Aria.MusicServers.MPD.UI.Connect;

public class ConnectDialogController(ConnectDialog dialog) : IBackendConnectDialogController
{
    private TaskCompletionSource<ConnectionDetails?> _tcs;

    public Widget View { get; }

    public async Task<ConnectionDetails?> ShowAsync(Widget? parent)
    {
        _tcs = new TaskCompletionSource<ConnectionDetails?>();

        dialog.ConnectClicked += DialogOnConnectClicked;
        dialog.CancelClicked += DialogOnCancelClicked;
        dialog.Present(parent);

        var result = await _tcs.Task;
        dialog.ConnectClicked -= DialogOnConnectClicked;
        dialog.CancelClicked -= DialogOnCancelClicked;

        return result;
    }

    private void DialogOnCancelClicked(object? sender, EventArgs e)
    {
        _tcs.SetResult(null);
    }

    private void DialogOnConnectClicked(object? sender, EventArgs e)
    {
        var details = new ConnectionDetails(dialog.Hostname, dialog.Port, dialog.Password);
        _tcs.SetResult(details);
    }

    public record ConnectionDetails(string? Hostname, int Port, string? Password);
}