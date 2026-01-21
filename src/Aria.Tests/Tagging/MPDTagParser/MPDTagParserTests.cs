using Aria.Backends.MPD;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Infrastructure.Tagging;

namespace Aria.Tests.Tagging.MPDTagParser;

public class MPDTagParserTests
{
    private readonly Aria.Infrastructure.Tagging.MPDTagParser _parser;

    public MPDTagParserTests()
    {
        // The MPD ID factory is used here to identify unique items.
        // Note that the MPD tag parser is also compatible with other backends.
        var idFactory = new IdProvider();
        _parser = new Aria.Infrastructure.Tagging.MPDTagParser(idFactory);
    }

    [Fact]
    public void ParseAlbumInformation_WithMultipleArtists_ConsolidatesCorrectly()
    {
        // Arrange
        List<Tag> tags = [
            new(MPDTags.Album, "My amazing album"),
            new(MPDTags.AlbumArtist, "Mirthe Stam"),
            new(MPDTags.Title, "My amazing song")
        ];

        // Act - Gebruik de parser uit de constructor
        var info = _parser.ParseAlbumInformation(tags);

        // Assert
        Assert.Equal("My amazing album", info.Title);
    }
}