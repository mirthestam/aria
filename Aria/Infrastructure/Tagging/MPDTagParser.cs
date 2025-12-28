using Aria.Core;
using Aria.Core.Library;

namespace Aria.Infrastructure.Tagging;

/// <summary>
///     A tag parser that follows tags as defined by the MPD best practices as documented
///     https://mpd.readthedocs.io/en/latest/protocol.html#tags
/// </summary>
/// <remarks>
///     This parser is in the common project, because applied tagging scheme is independent  of the loaded backend.
///     I plan to add other schemes, like  Lyrion, or pure Musicbrainz (Picard).
/// </remarks>
public class MPDTagParser : ITagParser
{
    // Meta
    private const string TagFile = "file";
    private const string TagDuration = "duration";
    private const string TagTitle = "title";
    private const string TagName = "name";
    private const string TagGenre = "genre";
    private const string TagComment = "comment";
    private const string TagId = "id";
    private const string TagDate = "date";

    // Album
    private const string TagAlbum = "album";
    private const string TagAlbumArtist = "albumartist";
    private const string TagTrack = "track";
    private const string TagDisc = "disc";
    private const string TagPos = "pos";

    // Artists
    private const string TagArtist = "artist";
    private const string TagComposer = "composer";
    private const string TagConductor = "conductor";
    private const string TagPerformer = "performer";
    private const string TagEnsemble = "ensemble";

    // Work
    private const string TagWork = "work";
    private const string TagMovement = "movement";
    private const string TagMovementNumber = "movementnumber";
    private const string TagShowMovement = "showmovement";

    // Recording
    private const string TagLocation = "location";

    public SongInfo ParseSongInformation(Id songId, IEnumerable<KeyValuePair<string, string>> tags)
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
        
        // TODO: Create a test project to test this parser based upon various scenario's
        // TODO: Implement support here for Sort tags for at all known roles
        foreach (var (tag, value) in tags)
            switch (tag.ToLowerInvariant())
            {
                case TagMovement:
                    movementNameTag = value;
                    break;

                case TagMovementNumber:
                    movementNumberTag = value;
                    break;

                case TagShowMovement:
                    showMovementTag = value == "1";
                    break;

                case TagArtist:
                    artistTags.Add(value);
                    break;

                case TagAlbumArtist:
                    albumArtistTags.Add(value);
                    break;

                case TagComposer:
                    composerTags.Add(value);
                    break;

                case TagTitle:
                    titleTag = value;
                    break;

                case TagPerformer:
                    performerTags.Add(value);
                    break;

                case TagConductor:
                    conductorTag = value;
                    break;

                case TagWork:
                    workTag = value;
                    break;

                case TagEnsemble:
                    ensembleTag = value;
                    break;

                case TagDuration:
                    var seconds = double.Parse(value);
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
            Id = songId,
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
            Duration = durationTag
        };

        return songInfo;

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
                artists.Add(new SongArtistInfo
                {
                    Roles = roles,
                    Artist = new ArtistInfo(Id.Empty, artistName, "", ArtistRoles.None)
                    // At this level, we don't know the roles for this artist. That is more relevant, when querying artists. 
                    // TODO: This should use an ID factory from the underlying backend implementation to generate proper domain identities. 
                });
            }
        }

        void AddAlbumArtist(string artistName)
        {
            var existingArtist = albumArtists.FirstOrDefault(a => a.Name == artistName);
            // TODO: This is the point where I can update Sort information  if it is present on another track,  but not yet known/            
            if (existingArtist != null) return; 

            albumArtists.Add(new ArtistInfo(Id.Empty, artistName, "", ArtistRoles.None));
        }
    }

    public AlbumInfo ParseAlbumInformation(Id albumId, IEnumerable<KeyValuePair<string, string>> tags)
    {
        var title = "";
        var tagList = tags.ToList();

        // TODO: Implement more fields. I.e. the date
        foreach (var (tag, value) in tagList)
            switch (tag.ToLowerInvariant())
            {
                case TagAlbum:
                    title = value;
                    break;
            }
        
        var songInfo = ParseSongInformation(Id.Empty, tagList);

        return new AlbumInfo
        {
            Id = albumId,
            Title = title,
            CreditsInfo = new AlbumCreditsInfo
            {
                Artists = songInfo.CreditsInfo.Artists,
                AlbumArtists = songInfo.CreditsInfo.AlbumArtists
            }
        };
    }
}