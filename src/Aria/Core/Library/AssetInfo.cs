using Aria.Core.Extraction;

namespace Aria.Core.Library;

public sealed record AssetInfo : Info
{
    /// <summary>
    /// The type of the album resource.
    /// </summary>
    public AssetType Type { get; init; }

    /// <summary>
    /// The MIME type of the album resource, representing the media type or format of the resource.
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// A brief description of the album resource. This property can be used to provide
    /// additional context or details about the resource.
    /// </summary>
    public string? Description { get; init; }
}