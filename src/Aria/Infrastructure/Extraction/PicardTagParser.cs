using System.Globalization;
using System.Text.RegularExpressions;
using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure.Extraction;

public sealed class ParsedTags
{
    public uint? QueuePosition { get; set; }

    public string FileName { get; set; } = "";
    public string Date { get; set; } = "";
    public string Title { get; set; } = "";
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    public string AlbumTitle { get; set; } = "";

    public string Work { get; set; } = "";
    public string MovementName { get; set; } = "";
    public string MovementNumber { get; set; } = "";
    public bool ShowMovement { get; set; }

    public string Disc { get; set; } = "";
    public string TrackNumber { get; set; } = "";
    public string Heading { get; set; } = "";

    public List<string> ArtistTags { get; } = new();
    public List<string> AlbumArtistTags { get; } = new();
    public List<string> ComposerTags { get; } = new();
    public List<string> PerformerTags { get; } = new();
    public List<string> EnsembleTags { get; } = new();
    public string Conductor { get; set; } = "";
}


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
    
    public QueueTrackInfo ParseQueueTrackInformation(IReadOnlyList<Tag> tags)
    {
        var parsed = ParseTags(tags);
        
        if (parsed.QueuePosition is null) throw new InvalidOperationException("Position tag is missing");

        var credits = BuildCredits(parsed);

        var trackInfo = new TrackInfo
        {
            FileName = parsed.FileName,
            CreditsInfo = credits.TrackCredits,
            Work = new WorkInfo
            {
                Work = parsed.Work,
                MovementName = parsed.MovementName,
                MovementNumber = parsed.MovementNumber,
                ShowMovement = parsed.ShowMovement
            },
            Title = parsed.Title,
            Duration = parsed.Duration,
            ReleaseDate = DateTagParser.ParseDate(parsed.Date),
            AlbumId = Id.Undetermined,
            Id = Id.Undetermined
        };

        var trackId = idProvider.CreateTrackId(new TrackBaseIdentificationContext { Track = trackInfo });
        trackInfo = trackInfo with { Id = trackId };

        var albumInfo = BuildAlbumInfo(parsed, trackInfo);
        trackInfo = trackInfo with { AlbumId = albumInfo.Id };

        var queueTrackInfo = new QueueTrackInfo
        {
            Id = Id.Undetermined,
            Position = parsed.QueuePosition.Value,
            Track = trackInfo
        };

        var queueTrackId = idProvider.CreateQueueTrackId(new QueueTrackBaseIdentificationContext
        {
            Tags = tags
        });

        return queueTrackInfo with { Id = queueTrackId };
    }

    public AlbumTrackInfo ParseAlbumTrackInformation(IReadOnlyList<Tag> tags)
    {
        var parsed = ParseTags(tags);

        var trackInfo = ParseTrackInformation(tags);

        int? trackNumber = null;
        if (int.TryParse(parsed.TrackNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tn))
            trackNumber = tn;

        TrackGroup? group = null;
        if (!string.IsNullOrWhiteSpace(parsed.Heading))
        {
            group = new TrackGroup
            {
                Header = parsed.Heading,
                Key = parsed.Heading
            };
        }
        
        return new AlbumTrackInfo
        {
            TrackNumber = trackNumber,
            VolumeName = parsed.Disc,
            Track = trackInfo,
            Group = group,
            Id = trackInfo.Id
        };
    }

    public AlbumInfo ParseAlbumInformation(IReadOnlyList<Tag> tags)
    {
        var parsed = ParseTags(tags);
        
        var credits = BuildCredits(parsed);

        var trackInfoForAlbumCredits = new TrackInfo
        {
            FileName = parsed.FileName,
            CreditsInfo = credits.TrackCredits,
            Work = new WorkInfo
            {
                Work = parsed.Work,
                MovementName = parsed.MovementName,
                MovementNumber = parsed.MovementNumber,
                ShowMovement = parsed.ShowMovement
            },
            Title = parsed.Title,
            Duration = parsed.Duration,
            ReleaseDate = DateTagParser.ParseDate(parsed.Date),
            AlbumId = Id.Undetermined,
            Id = Id.Undetermined
        };

        var albumInfo = new AlbumInfo
        {
            Title = parsed.AlbumTitle,
            CreditsInfo = new AlbumCreditsInfo
            {
                Artists = trackInfoForAlbumCredits.CreditsInfo.Artists,
                AlbumArtists = trackInfoForAlbumCredits.CreditsInfo.AlbumArtists
            },
            ReleaseDate = trackInfoForAlbumCredits.ReleaseDate,
            Id = Id.Undetermined
        };

        var id = idProvider.CreateAlbumId(new AlbumBaseIdentificationContext { Album = albumInfo });
        return albumInfo with { Id = id };
    }

    public ArtistInfo ParseArtistInformation(string artistName, string? artistNameSort, ArtistRoles roles)
    {
        var artistNameParts = ParseArtistNameParts(artistName);
        var artistNameSortParts = ParseArtistNameParts(artistNameSort);

        return new ArtistInfo
        {
            Name = artistNameParts?.Name ?? artistName,
            NameSort = artistNameSortParts?.Extra ?? artistNameSort,
            Roles = roles,
            Id = Id.Undetermined
        };
    }
    
    private static ParsedTags ParseTags(IReadOnlyList<Tag> tags)
    {
        var parsed = new ParsedTags();

        foreach (var tag in tags)
        {
            if (tag.Name.Equals(PicardTagNames.QueueTags.Position, StringComparison.OrdinalIgnoreCase))
            {
                if (uint.TryParse(tag.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pos))
                    parsed.QueuePosition = pos;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.FileTags.File, StringComparison.OrdinalIgnoreCase))
            {
                parsed.FileName = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.TrackTags.Date, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Date = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.TrackTags.Duration, StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(tag.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                    parsed.Duration = TimeSpan.FromSeconds(seconds);
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.TrackTags.Title, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Title = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.AlbumTags.Album, StringComparison.OrdinalIgnoreCase))
            {
                parsed.AlbumTitle = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.WorkTags.Work, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Work = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.WorkTags.Movement, StringComparison.OrdinalIgnoreCase))
            {
                parsed.MovementName = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.WorkTags.MovementNumber, StringComparison.OrdinalIgnoreCase))
            {
                parsed.MovementNumber = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.WorkTags.ShowMovement, StringComparison.OrdinalIgnoreCase))
            {
                parsed.ShowMovement = tag.Value == "1";
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.ArtistTags.Artist, StringComparison.OrdinalIgnoreCase))
            {
                parsed.ArtistTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.AlbumTags.AlbumArtist, StringComparison.OrdinalIgnoreCase))
            {
                parsed.AlbumArtistTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.ArtistTags.Composer, StringComparison.OrdinalIgnoreCase))
            {
                parsed.ComposerTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.ArtistTags.Performer, StringComparison.OrdinalIgnoreCase))
            {
                parsed.PerformerTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.ArtistTags.Ensemble, StringComparison.OrdinalIgnoreCase))
            {
                parsed.EnsembleTags.Add(tag.Value);
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.ArtistTags.Conductor, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Conductor = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.AlbumTags.Disc, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Disc = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.AlbumTags.Track, StringComparison.OrdinalIgnoreCase))
            {
                parsed.TrackNumber = tag.Value;
                continue;
            }

            if (tag.Name.Equals(PicardTagNames.GroupTags.Heading, StringComparison.OrdinalIgnoreCase))
            {
                parsed.Heading = tag.Value;
            }
        }

        return parsed;
    }

    private sealed record BuiltCredits(TrackCreditsInfo TrackCredits);

    private BuiltCredits BuildCredits(ParsedTags parsed)
    {
        var albumArtists = new List<ArtistInfo>();
        var artists = new List<TrackArtistInfo>();
        
        foreach (var s in parsed.ArtistTags)
            if (!string.IsNullOrWhiteSpace(s))
                AddArtist(s, ArtistRoles.None);

        foreach (var s in parsed.AlbumArtistTags)
            if (!string.IsNullOrWhiteSpace(s))
                AddAlbumArtist(s);

        foreach (var s in parsed.ComposerTags)
            if (!string.IsNullOrWhiteSpace(s))
                AddArtist(s, ArtistRoles.Composer);

        foreach (var s in parsed.PerformerTags)
            if (!string.IsNullOrWhiteSpace(s))
                AddArtist(s, ArtistRoles.Performer);

        foreach (var s in parsed.EnsembleTags)
            if (!string.IsNullOrWhiteSpace(s))
                AddArtist(s, ArtistRoles.Ensemble);

        if (!string.IsNullOrWhiteSpace(parsed.Conductor))
            AddArtist(parsed.Conductor, ArtistRoles.Conductor);

        return new BuiltCredits(new TrackCreditsInfo
        {
            Artists = artists.AsReadOnly(),
            AlbumArtists = albumArtists.AsReadOnly()
        });

        void AddArtist(string artistName, ArtistRoles roles)
        {
            var parts = ParseArtistNameParts(artistName);

            var existingArtist = artists.FirstOrDefault(a => a.Artist.Name == parts.Name);
            if (existingArtist != null)
            {
                var index = artists.IndexOf(existingArtist);
                artists[index] = existingArtist with { Roles = existingArtist.Roles | roles };
                return;
            }

            var artistInfo = new TrackArtistInfo
            {
                Roles = roles,
                Artist = new ArtistInfo
                {
                    Name = parts.Name,
                    Id = Id.Empty
                },
                AdditionalInformation = parts.Extra
            };

            var artistId = idProvider.CreateArtistId(new ArtistBaseIdentificationContext { Artist = artistInfo.Artist });
            artistInfo = artistInfo with { Artist = artistInfo.Artist with { Id = artistId } };

            artists.Add(artistInfo);
        }

        void AddAlbumArtist(string artistName)
        {
            var existingArtist = albumArtists.FirstOrDefault(a => a.Name == artistName);
            if (existingArtist != null) return;

            var artistInfo = new ArtistInfo
            {
                Name = artistName,
                Roles = ArtistRoles.None,
                Id = Id.Undetermined
            };

            var artistId = idProvider.CreateArtistId(new ArtistBaseIdentificationContext { Artist = artistInfo });
            albumArtists.Add(artistInfo with { Id = artistId });
        }
    }

    private AlbumInfo BuildAlbumInfo(ParsedTags parsed, TrackInfo trackInfo)
    {
        var albumInfo = new AlbumInfo
        {
            Title = parsed.AlbumTitle,
            CreditsInfo = new AlbumCreditsInfo
            {
                Artists = trackInfo.CreditsInfo.Artists,
                AlbumArtists = trackInfo.CreditsInfo.AlbumArtists
            },
            ReleaseDate = trackInfo.ReleaseDate,
            Id = Id.Undetermined
        };

        var albumId = idProvider.CreateAlbumId(new AlbumBaseIdentificationContext { Album = albumInfo });
        return albumInfo with { Id = albumId };
    }

    private TrackInfo ParseTrackInformation(IReadOnlyList<Tag> tags)
    {
        var parsed = ParseTags(tags);

        var credits = BuildCredits(parsed);

        var trackInfo = new TrackInfo
        {
            FileName = parsed.FileName,
            CreditsInfo = credits.TrackCredits,
            Work = new WorkInfo
            {
                Work = parsed.Work,
                MovementName = parsed.MovementName,
                MovementNumber = parsed.MovementNumber,
                ShowMovement = parsed.ShowMovement
            },
            Title = parsed.Title,
            Duration = parsed.Duration,
            ReleaseDate = DateTagParser.ParseDate(parsed.Date),
            AlbumId = Id.Undetermined,
            Id = Id.Undetermined
        };

        var trackId = idProvider.CreateTrackId(new TrackBaseIdentificationContext { Track = trackInfo });
        trackInfo = trackInfo with { Id = trackId };

        var albumInfo = BuildAlbumInfo(parsed, trackInfo);
        return trackInfo with { AlbumId = albumInfo.Id };
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