using Aria.Core;
using Aria.Core.Library;

namespace Aria.Infrastructure.Tagging;


public static class MPDTags
{
    public const string TagFile = "file";
    public const string TagDuration = "duration";
    public const string TagTitle = "title";
    public const string TagName = "name";
    public const string TagGenre = "genre";
    public const string TagComment = "comment";
    public const string TagId = "id";
    public const string TagDate = "date";

    // Album
    public const string TagAlbum = "album";
    public const string TagAlbumArtist = "albumartist";
    public const string TagTrack = "track";
    public const string TagDisc = "disc";
    public const string TagPos = "pos";

    // Artists
    public const string TagArtist = "artist";
    public const string TagComposer = "composer";
    public const string TagConductor = "conductor";
    public const string TagPerformer = "performer";
    public const string TagEnsemble = "ensemble";

    // Work
    public const string TagWork = "work";
    public const string TagMovement = "movement";
    public const string TagMovementNumber = "movementnumber";
    public const string TagShowMovement = "showmovement";
    
    // Recording
    public const string TagLocation = "location";
}

/// <summary>
///     A tag parser that follows tags as defined by the MPD best practices as documented
///     https://mpd.readthedocs.io/en/latest/protocol.html#tags
/// </summary>
/// <remarks>
///     This parser is in the common project, because applied tagging scheme is independent  of the loaded backend.
///     I plan to add other schemes, like  Lyrion, or pure Musicbrainz (Picard).
/// </remarks>
public class MPDTagParser(IIdFactory idFactory) : ITagParser
{
    public SongInfo ParseSongInformation(IReadOnlyList<Tag> tags)
    {
        var artistTags = new List<string>();
        var albumArtistTags = new List<string>();
        var composerTags = new List<string>();
        var performerTags = new List<string>();
        var titleTag = "";
        var conductorTag = "";
        var ensembleTag = "";
        var workTag = "";
        var movementNameTag = "";
        var movementNumberTag = "";
        var showMovementTag = false;
        var durationTag = TimeSpan.Zero;
        var dateTag = "";
        var fileNameTag = "";

        // TODO: Create a test project to test this parser based upon various scenario's
        // TODO: Implement support here for Sort tags for at all known roles
        foreach (var tag in tags)
            switch (tag.Name.ToLowerInvariant())
            {
                case MPDTags.TagFile:
                    fileNameTag = tag.Value;
                    break;
                
                case MPDTags.TagDate:
                    dateTag = tag.Value;
                    break;
                
                case MPDTags.TagMovement:
                    movementNameTag = tag.Value;
                    break;

                case MPDTags.TagMovementNumber:
                    movementNumberTag = tag.Value;
                    break;

                case MPDTags.TagShowMovement:
                    showMovementTag = tag.Value == "1";
                    break;

                case MPDTags.TagArtist:
                    artistTags.Add(tag.Value);
                    break;

                case MPDTags.TagAlbumArtist:
                    albumArtistTags.Add(tag.Value);
                    break;

                case MPDTags.TagComposer:
                    composerTags.Add(tag.Value);
                    break;

                case MPDTags.TagTitle:
                    titleTag = tag.Value;
                    break;

                case MPDTags.TagPerformer:
                    performerTags.Add(tag.Value);
                    break;

                case MPDTags.TagConductor:
                    conductorTag = tag.Value;
                    break;

                case MPDTags.TagWork:
                    workTag = tag.Value;
                    break;

                case MPDTags.TagEnsemble:
                    ensembleTag = tag.Value;
                    break;

                case MPDTags.TagDuration:
                    var seconds = double.Parse(tag.Value);
                    durationTag = TimeSpan.FromSeconds(seconds);
                    break;
            }

        var albumArtists = new List<ArtistInfo>();
        var artists = new List<SongArtistInfo>();

        foreach (var artistName in artistTags.Where(artistName => !string.IsNullOrWhiteSpace(artistName)))
            AddArtist(artistName, ArtistRoles.None);

        foreach (var artistName in albumArtistTags.Where(artistName => !string.IsNullOrWhiteSpace(artistName)))
            AddAlbumArtist(artistName);

        foreach (var composerName in composerTags.Where(composerName => !string.IsNullOrWhiteSpace(composerName)))
            AddArtist(composerName, ArtistRoles.Composer);

        foreach (var performerName in performerTags.Where(performerName => !string.IsNullOrWhiteSpace(performerName)))
            AddArtist(performerName, ArtistRoles.Performer);

        if (!string.IsNullOrWhiteSpace(ensembleTag)) AddArtist(ensembleTag, ArtistRoles.Ensemble);

        if (!string.IsNullOrWhiteSpace(conductorTag)) AddArtist(conductorTag, ArtistRoles.Conductor);
        
        var songInfo = new SongInfo
        {
            FileName = fileNameTag,
            CreditsInfo = new SongCreditsInfo
            {
                Artists = artists.AsReadOnly(),
                AlbumArtists = albumArtists.AsReadOnly()
            },
            Work = new WorkInfo
            {
                Work = workTag,
                MovementName = movementNameTag,
                MovementNumber = movementNumberTag,
                ShowMovement = showMovementTag
            },
            Title = titleTag,
            Duration = durationTag,
            ReleaseDate = DateTagParser.ParseDate(dateTag)
        };
        
        var songId = idFactory.CreateSongId(new SongTagParserContext
        {
            Song = songInfo
        });        

        return songInfo with { Id = songId };

        void AddArtist(string artistName, ArtistRoles roles)
        {
            var existingArtist = artists.FirstOrDefault(a => a.Artist.Name == artistName);
            if (existingArtist != null)
            {
                var index = artists.IndexOf(existingArtist);
                artists[index] = existingArtist with
                {
                    Roles = existingArtist.Roles | roles
                };
            }
            else
            {
                var artistInfo = new SongArtistInfo
                {
                    Roles = roles,
                    Artist = new ArtistInfo
                    {
                        Name = artistName
                    }
                };
                var artistId = idFactory.CreateArtistId(new ArtistTagParserContext
                {
                    Artist = artistInfo.Artist
                });

                artistInfo = artistInfo with
                {
                    Artist = artistInfo.Artist with { Id = artistId }
                };

                artists.Add(artistInfo);
            }
        }

        void AddAlbumArtist(string artistName)
        {
            var existingArtist = albumArtists.FirstOrDefault(a => a.Name == artistName);
            // TODO: This is the point where I can update Sort information  if it is present on another track,  but not yet known/            
            if (existingArtist != null) return;

            var artistInfo = new ArtistInfo
            {
                Name = artistName,
                Roles = ArtistRoles.None
            };
            var artistId = idFactory.CreateArtistId(new ArtistTagParserContext
            {
                Artist = artistInfo
            });

            artistInfo = artistInfo with
            {
                Id = artistId
            };

            albumArtists.Add(artistInfo);
        }
    }

    public AlbumInfo ParseAlbumInformation(IReadOnlyList<Tag> tags)
    {
        var title = "";
        var tagList = tags.ToList();

        // TODO: Implement more fields. I.e. the date
        foreach (var (tag, value) in tagList)
            switch (tag.ToLowerInvariant())
            {
                case MPDTags.TagAlbum:
                    title = value;
                    break;
            }

        var songInfo = ParseSongInformation(tagList);

        var albumInfo = new AlbumInfo
        {
            Title = title,
            CreditsInfo = new AlbumCreditsInfo
            {
                Artists = songInfo.CreditsInfo.Artists,
                AlbumArtists = songInfo.CreditsInfo.AlbumArtists
            },
            ReleaseDate = songInfo.ReleaseDate
        };

        var id = idFactory.CreateAlbumId(new AlbumTagParserContext
        {
            Album = albumInfo
        });

        return albumInfo with { Id = id };
    }
}