namespace Aria.Infrastructure;

public static class GtkDispatch
{
    /// <summary>
    /// Run an action on the GTK main loop (idle) and complete when it has executed.
    /// </summary>
    public static Task InvokeIdleAsync(Action action, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        GLib.Functions.IdleAdd(0, () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return false;
            }

            try
            {
                action();
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return false;
        });

        return tcs.Task;
    }
}