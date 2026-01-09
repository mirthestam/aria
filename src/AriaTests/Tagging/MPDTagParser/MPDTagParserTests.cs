using Aria.Infrastructure.Tagging;
using Aria.MusicServers.MPD;

namespace AriaTests.Tagging.MPDTagParser;

public class MPDTagParserTests
{
    private readonly Aria.Infrastructure.Tagging.MPDTagParser _parser;

    public MPDTagParserTests()
    {
        // The MPD ID factory is used here to identify unique items.
        // Note that the MPD tag parser is also compatible with other backends.
        var idFactory = new IdFactory();
        _parser = new Aria.Infrastructure.Tagging.MPDTagParser(idFactory);
    }

    [Fact]
    public void ParseAlbumInformation_WithMultipleArtists_ConsolidatesCorrectly()
    {
        // Arrange
        List<Tag> tags = [
            new(MPDTags.TagAlbum, "My amazing album"),
            new(MPDTags.TagAlbumArtist, "Mirthe Stam"),
            new(MPDTags.TagTitle, "My amazing song")
        ];

        // Act - Gebruik de parser uit de constructor
        var info = _parser.ParseAlbumInformation(tags);

        // Assert
        Assert.Equal("My amazing album", info.Title);
    }
}