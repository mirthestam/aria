using Aria.Backends.MPD;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Infrastructure.Extraction;

namespace Aria.Tests.Tagging.MPDTagParser;

public class PicardTagParserTests
{
    private readonly Infrastructure.Extraction.PicardTagParser _parser;

    public PicardTagParserTests()
    {
        // The MPD ID factory is used here to identify unique items.
        // Note that the MPD tag parser is also compatible with other backends.
        var idFactory = new IdProvider();
        _parser = new Infrastructure.Extraction.PicardTagParser(idFactory);
    }

    [Fact]
    public void ParseAlbumInformation_WithMultipleArtists_ConsolidatesCorrectly()
    {
        // Arrange
        List<Tag> tags = [
            new(PicardTagNames.AlbumTags.Album, "My amazing album"),
            new(PicardTagNames.AlbumTags.AlbumArtist, "Mirthe Stam"),
            new(PicardTagNames.TrackTags.Title, "My amazing song")
        ];

        // Act - Gebruik de parser uit de constructor
        var info = _parser.ParseAlbumInformation(tags);

        // Assert
        Assert.Equal("My amazing album", info.Title);
    }
}