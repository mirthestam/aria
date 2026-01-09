using Aria.Core;
using Aria.Core.Library;
using Gdk;
using Microsoft.Extensions.Logging;

namespace Aria.Infrastructure;

public partial class ResourceTextureLoader(ILogger<ResourceTextureLoader> logger, ILibrary library)
{
    public async Task<Texture?> LoadFromAlbumResourceAsync(Id resourceId)
    {
        try
        {
            await using var stream = await library.GetAlbumResourceStreamAsync(resourceId);
            if (stream == Stream.Null)
            {
                LogResourceNotFound(resourceId);
                return null;
            }

            using var loader = new GdkPixbuf.PixbufLoader();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            
            loader.Write(ms.ToArray());
            loader.Close();

            using var pixbuf = loader.GetPixbuf();
            if (pixbuf != null) return Texture.NewForPixbuf(pixbuf);
            
            LogCouldNotDecodeResource(resourceId);
            return null;

        }
        catch (Exception ex)
        {
            LogExceptionLoadingResource(ex, resourceId);
            return null;
        }
    }
    
    [LoggerMessage(LogLevel.Warning, "Resource {resourceId} not found in library")]
    partial void LogResourceNotFound(Id resourceId);

    [LoggerMessage(LogLevel.Error, "Could not decode resource {resourceId} as an image")]
    partial void LogCouldNotDecodeResource(Id resourceId);

    [LoggerMessage(LogLevel.Error, "Exception while loading resource {resourceId}")]
    partial void LogExceptionLoadingResource(Exception ex, Id resourceId);
}