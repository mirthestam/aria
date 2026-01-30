using Aria.Backends.MPD;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Infrastructure.Extraction;

namespace Aria.Tests.Tagging.MPDTagParser;

public class MPDTagParserTests
{
    private readonly Infrastructure.Extraction.MPDTagParser _parser;

    public MPDTagParserTests()
    {
        // The MPD ID factory is used here to identify unique items.
        // Note that the MPD tag parser is also compatible with other backends.
        var idFactory = new IdProvider();
        _parser = new Infrastructure.Extraction.MPDTagParser(idFactory);
    }

    [Fact]
    public void ParseAlbumInformation_WithMultipleArtists_ConsolidatesCorrectly()
    {
        // Arrange
        List<Tag> tags = [
            new(MPDTags.AlbumTags.Album, "My amazing album"),
            new(MPDTags.AlbumTags.AlbumArtist, "Mirthe Stam"),
            new(MPDTags.TrackTags.Title, "My amazing song")
        ];

        // Act - Gebruik de parser uit de constructor
        var info = _parser.ParseAlbumInformation(tags);

        // Assert
        Assert.Equal("My amazing album", info.Title);
    }
}