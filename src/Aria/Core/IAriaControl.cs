using Aria.Core.Extraction;
using Aria.Infrastructure;

namespace Aria.Core;

public interface IAriaControl
{
    public Task InitializeAsync();

    public Task StartAsync(Guid profileId, CancellationToken cancellationToken = default);
    
    public Task StopAsync();
    
    public Id Parse(string id);    
    
    event EventHandler<EngineStateChangedEventArgs>? StateChanged;
}