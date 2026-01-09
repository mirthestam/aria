using System.Diagnostics.CodeAnalysis;
using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Aria.Infrastructure.Tagging;

namespace Aria.Infrastructure;

[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")] 
public abstract class BackendConnection : IBackendConnection
{
    /// <summary>
    /// Gets access to the  Tar Parser that the user wants to use to interpret file tags with
    /// </summary>
    public abstract ITagParser TagParser { get; }
    

    public virtual bool IsConnected => false;

    public abstract IPlayer Player { get; }

    public abstract IPlaylist Playlist { get; }

    public abstract ILibrary Library { get; }
    
    public abstract Task InitializeAsync();
    
    public abstract Task DisconnectAsync();

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}