namespace Aria.Core;

public interface IAriaControl
{
    public Task InitializeAsync();

    public Task ConnectAsync(Guid profileId, CancellationToken cancellationToken = default);
    
    public Task DisconnectAsync();
}