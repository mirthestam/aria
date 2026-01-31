using System.Text.RegularExpressions;
using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure.Extraction;

/// <summary>
///     A tag parser that follows tags as defined by the default configuration of MusicBrainz Picard
///     https://mpd.readthedocs.io/en/latest/protocol.html#tags
/// </summary>


/* About Album Artists
 https://community.metabrainz.org/t/multiple-album-artists/532302/13
 Picard has no default 'albumartists' multi-list. Also, for 'albumartist'  join phrases do not seem to be standardized.
 
 This script, as proposed in the topic above, converts it to a multi list. MPD supports this multi list. Therefore,
 we now CAN handle them.
 
 $setmulti(albumartist,%_albumartists%)
 $setmulti(albumartistsort,%_albumartists_sort%)
   
   
If you want to be able to see ensembles in the artists list,
you'll need to use the calssic tool to map 'ensemble_names' field to 'ensemble'.
Do note, this is a single field.

// Ik heb nog afwijkende namen in album artist.
https://github.com/rdswift/picard-plugins/blob/2.0_RDS_Plugins/plugins/additional_artists_variables/docs/README.md
die plugin proberen, EN dan documenteren
 */

public partial class PicardTagParser(IIdProvider idProvider) : ITagParser
{
    private sealed record ParsedArtistName(string Name, string? Extra);
    
    private static readonly Regex PerformerSuffixRegex = PerformerSuffixRegexFactory();
    
    public TrackInfo ParseTrackInformation(IReadOnlyList<Tag> tags)
    {
        var artistTags = new List<string>();
        var albumArtistTags = new List<string>();
        var composerTags = new List<string>();     
        var performerTags = new List<string>();
        var ensembleTags = new List<string>();
        var titleTag = "";
        var conductorTag = "";
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
                case PicardTagNames.FileTags.File:
                    fileNameTag = tag.Value;
                    break;
                
                case PicardTagNames.TrackTags.Date:
                    dateTag = tag.Value;
                    break;
                
                case PicardTagNames.WorkTags.Movement:
                    movementNameTag = tag.Value;
                    break;

                case PicardTagNames.WorkTags.MovementNumber:
                    movementNumberTag = tag.Value;
                    break;

                case PicardTagNames.WorkTags.ShowMovement:
                    showMovementTag = tag.Value == "1";
                    break;

                case PicardTagNames.ArtistTags.Artist:
                    artistTags.Add(tag.Value);
                    break;

                case PicardTagNames.AlbumTags.AlbumArtist:
                    albumArtistTags.Add(tag.Value);
                    break;

                case PicardTagNames.ArtistTags.Composer:
                    composerTags.Add(tag.Value);
                    break;

                case PicardTagNames.TrackTags.Title:
                    titleTag = tag.Value;
                    break;

                case PicardTagNames.ArtistTags.Performer:
                    performerTags.Add(tag.Value);
                    break;
                
                case PicardTagNames.ArtistTags.Ensemble:
                    ensembleTags.Add(tag.Value);
                    break;

                case PicardTagNames.ArtistTags.Conductor:
                    conductorTag = tag.Value;
                    break;

                case PicardTagNames.WorkTags.Work:
                    workTag = tag.Value;
                    break;
                
                case PicardTagNames.TrackTags.Duration:
                    var seconds = double.Parse(tag.Value);
                    durationTag = TimeSpan.FromSeconds(seconds);
                    break;
            }

        var albumArtists = new List<ArtistInfo>();
        var artists = new List<TrackArtistInfo>();

        foreach (var artistName in artistTags.Where(artistName => !string.IsNullOrWhiteSpace(artistName)))
            AddArtist(artistName, ArtistRoles.None);

        foreach (var artistName in albumArtistTags.Where(artistName => !string.IsNullOrWhiteSpace(artistName)))
            AddAlbumArtist(artistName);

        foreach (var composerName in composerTags.Where(composerName => !string.IsNullOrWhiteSpace(composerName)))
            AddArtist(composerName, ArtistRoles.Composer);

        foreach (var performerName in performerTags.Where(performerName => !string.IsNullOrWhiteSpace(performerName)))
            AddArtist(performerName, ArtistRoles.Performer);

        foreach (var ensembleName in ensembleTags.Where(ensembleName => !string.IsNullOrWhiteSpace(ensembleName)))
            AddArtist(ensembleName, ArtistRoles.Ensemble);

        if (!string.IsNullOrWhiteSpace(conductorTag)) AddArtist(conductorTag, ArtistRoles.Conductor);
        
        var trackInfo = new TrackInfo
        {
            FileName = fileNameTag,
            CreditsInfo = new TrackCreditsInfo
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
        
        var trackId = idProvider.CreateTrackId(new TrackIdentificationContext
        {
            Track = trackInfo
        });        

        return trackInfo with { Id = trackId };

        void AddArtist(string artistName, ArtistRoles roles)
        {

            var parts = ParseArtistNameParts(artistName);
            
            var existingArtist = artists.FirstOrDefault(a => a.Artist.Name == parts.Name);
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
                var artistInfo = new TrackArtistInfo
                {
                    Roles = roles,
                    Artist = new ArtistInfo
                    {
                        Name = parts.Name
                    },
                    AdditionalInformation = parts.Extra
                };
                var artistId = idProvider.CreateArtistId(new ArtistIdentificationContext
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
            var artistId = idProvider.CreateArtistId(new ArtistIdentificationContext
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

    public QueueTrackInfo ParseQueueTrackInformation(IReadOnlyList<Tag> tags)
    {
        int? position = null;
        
        foreach (var tag in tags)
        {
            switch (tag.Name.ToLowerInvariant())
            {
                case PicardTagNames.QueueTags.Position:
                    position = int.Parse(tag.Value);
                    break;
            }
        }

        if (position == null)
        {
            throw new InvalidOperationException("Position tag is missing");
        }
        
        var trackInfo = ParseTrackInformation(tags);
        
        var queueTrackInfo = new QueueTrackInfo
        {
            Id = null,
            Position = position.Value,
            Track = trackInfo
        };

        var queueTrackId  = idProvider.CreateQueueTrackId(new QueueTrackIdentificationContext
        {
            Tags = tags,
            Track = queueTrackInfo
        });
        
        return queueTrackInfo with { Id = queueTrackId };        
    }

    public AlbumTrackInfo ParseAlbumTrackInformation(IReadOnlyList<Tag> tags)
    {
        var diskTag = "";
        var trackNumberTag = "";
        var headingTag = "";

        foreach (var tag in tags)
        {
            switch (tag.Name.ToLowerInvariant())
            {
                case PicardTagNames.AlbumTags.Track:
                    trackNumberTag = tag.Value;
                    break;
                
                case PicardTagNames.AlbumTags.Disc:
                    diskTag = tag.Value;
                    break;
                
                case PicardTagNames.GroupTags.Heading:
                    headingTag = tag.Value;
                    break;
            }
        }
        
        var trackInfo = ParseTrackInformation(tags);
        var isTrackNumberFound = int.TryParse(trackNumberTag, out var trackNumber);

        TrackGroup? group = null;
        
        if (!string.IsNullOrWhiteSpace(headingTag))
        {
            group = new TrackGroup
            {
                Title = headingTag,
                Key = headingTag
            };
        }
        
        var albumTrackInfo = new AlbumTrackInfo
        {
            TrackNumber = isTrackNumberFound ? trackNumber : null,
            VolumeName = diskTag,
            Track = trackInfo,
            Group = group
        };
        
        return albumTrackInfo;
    }
    
    public AlbumInfo ParseAlbumInformation(IReadOnlyList<Tag> tags)
    {
        var title = "";
        var tagList = tags.ToList();

        // TODO: Implement more fields. I.e. the date
        foreach (var (tag, value) in tagList)
            switch (tag.ToLowerInvariant())
            {
                case PicardTagNames.AlbumTags.Album:
                    title = value;
                    break;
            }

        var trackInfo = ParseTrackInformation(tagList);

        var albumInfo = new AlbumInfo
        {
            Title = title,
            CreditsInfo = new AlbumCreditsInfo
            {
                Artists = trackInfo.CreditsInfo.Artists,
                AlbumArtists = trackInfo.CreditsInfo.AlbumArtists
            },
            ReleaseDate = trackInfo.ReleaseDate
        };

        var id = idProvider.CreateAlbumId(new AlbumIdentificationContext
        {
            Album = albumInfo
        });

        return albumInfo with { Id = id };
    }

    public ArtistInfo? ParseArtistInformation(string artistName, string? artistNameSort, ArtistRoles roles)
    {
        var artistNameParts = ParseArtistNameParts(artistName);
        var artistNameSortParts = ParseArtistNameParts(artistNameSort);
        
        return new ArtistInfo
        {
            Name = artistNameParts?.Name ?? artistName,
            NameSort = artistNameSortParts?.Extra ?? artistNameSort,
            Roles = roles
        };
    }
 
    private static ParsedArtistName? ParseArtistNameParts(string? value)
    {
        // Picard uses 'Name (Extra)' format for artists.'
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        
        var match = PerformerSuffixRegex.Match(trimmed);
        if (!match.Success)
            return new ParsedArtistName(trimmed, null);

        var name = match.Groups["name"].Value.Trim();
        var extra = match.Groups["extra"].Value.Trim();
        
        return string.IsNullOrWhiteSpace(name)
            ? new ParsedArtistName(trimmed, null)
            : new ParsedArtistName(name, string.IsNullOrWhiteSpace(extra) ? null : extra);
    }    
    
    [GeneratedRegex(@"^(?<name>.*?)\s*\((?<extra>[^()]+)\)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PerformerSuffixRegexFactory();
}