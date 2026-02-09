using Aria.Core.Extraction;
using Aria.Core.Library;
using Gdk;
using Microsoft.Extensions.Logging;

namespace Aria.Infrastructure;

public partial class ResourceTextureLoader(ILogger<ResourceTextureLoader> logger, ILibrary library)
{
    public async Task<Texture?> LoadFromAlbumResourceAsync(Id resourceId, CancellationToken cancellationToken = default)
    {
        using var pixelBufferLoader = GdkPixbuf.PixbufLoader.NewWithProperties([]);
        try
        {
            await using var stream = await library.GetAlbumResourceStreamAsync(resourceId, cancellationToken)
                .ConfigureAwait(false);
            if (stream == Stream.Null)
            {
                LogResourceNotFound(resourceId);
                return null;
            }

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);

            pixelBufferLoader.Write(ms.ToArray());
            pixelBufferLoader.Close();

            using var pixelBuffer = pixelBufferLoader.GetPixbuf();
            if (pixelBuffer != null) return Texture.NewForPixbuf(pixelBuffer);

            LogCouldNotDecodeResource(resourceId);
            return null;

        }
        catch (OperationCanceledException)
        {
            try
            {
                pixelBufferLoader.Close();
            }
            catch
            {
                // Ignored.
                // The pixelBufferLoader is closed before cancellation to avoid GTK warnings.
                // If it is closed while loading is still in progress, it may throw exceptions
                // (e.g., "Unrecognized image file format").
                // This is expected, as the load operation was canceled midway.
            }            
            throw;
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            
            LogExceptionLoadingResource(ex, resourceId);
            try
            {
                pixelBufferLoader.Close();
            }
            catch(Exception innerEx)
            {
                LogFailedToClosePixbufLoader(logger, innerEx);
            }
            return null;
        }

    }
    
    [LoggerMessage(LogLevel.Warning, "Resource {resourceId} not found in library")]
    partial void LogResourceNotFound(Id resourceId);

    [LoggerMessage(LogLevel.Error, "Could not decode resource {resourceId} as an image")]
    partial void LogCouldNotDecodeResource(Id resourceId);

    [LoggerMessage(LogLevel.Error, "Exception while loading resource {resourceId}")]
    partial void LogExceptionLoadingResource(Exception ex, Id resourceId);

    [LoggerMessage(LogLevel.Warning, "Failed to close pixbuf loader")]
    static partial void LogFailedToClosePixbufLoader(ILogger<ResourceTextureLoader> logger, Exception e);
}