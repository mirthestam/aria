using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

public class IdProvider : IIdProvider
{
    public Id CreateTrackId(TrackIdentificationContext context) => TrackId.FromContext(context);

    public Id CreateArtistId(ArtistIdentificationContext context) => ArtistId.FromContext(context);

    public Id CreateAlbumId(AlbumIdentificationContext context) => AlbumId.FromContext(context);


public Id Parse(string id)
{
    // Getting the parts could be a function in a IdProviderBase
    
    // Remove surrounding single quotes if present (handles escaped/quoted values)
    // This should either remove ' or "
    if (id is (['\'', _, ..] and [.., '\'']) or (['"', _, ..] and [.., '"']))
    {
        id = id[1..^1];
    }    
    
    var parts = id.Split("::", 2);
    if (parts.Length < 2) throw new ArgumentException("Invalid ID format");
    
    var value = parts[1];

    // Route to the appropriate ID parser based on the key prefix
        return parts[0] switch
        {
            ArtistId.Key => ArtistId.Parse(value),
            _ => throw new NotSupportedException($"Unknown ID key: `{parts[0]}`")
        };
    }
}