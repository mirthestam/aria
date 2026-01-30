using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure.Extraction;

/// <summary>
///     A tag parser that follows tags as defined by the MPD best practices as documented
///     https://mpd.readthedocs.io/en/latest/protocol.html#tags
/// </summary>
/// <remarks>
///     This parser is in the common project, because applied tagging scheme is independent  of the loaded backend.
///     I plan to add other schemes, like  Lyrion, or pure Musicbrainz (Picard).
/// </remarks>
public class MPDTagParser(IIdProvider idProvider) : ITagParser
{
    public TrackInfo ParseTrackInformation(IReadOnlyList<Tag> tags)
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
                case MPDTags.FileTags.File:
                    fileNameTag = tag.Value;
                    break;
                
                case MPDTags.TrackTags.Date:
                    dateTag = tag.Value;
                    break;
                
                case MPDTags.WorkTags.Movement:
                    movementNameTag = tag.Value;
                    break;

                case MPDTags.WorkTags.MovementNumber:
                    movementNumberTag = tag.Value;
                    break;

                case MPDTags.WorkTags.ShowMovement:
                    showMovementTag = tag.Value == "1";
                    break;

                case MPDTags.ArtistTags.Artist:
                    artistTags.Add(tag.Value);
                    break;

                case MPDTags.AlbumTags.AlbumArtist:
                    albumArtistTags.Add(tag.Value);
                    break;

                case MPDTags.ArtistTags.Composer:
                    composerTags.Add(tag.Value);
                    break;

                case MPDTags.TrackTags.Title:
                    titleTag = tag.Value;
                    break;

                case MPDTags.ArtistTags.Performer:
                    performerTags.Add(tag.Value);
                    break;

                case MPDTags.ArtistTags.Conductor:
                    conductorTag = tag.Value;
                    break;

                case MPDTags.WorkTags.Work:
                    workTag = tag.Value;
                    break;

                case MPDTags.ArtistTags.Ensemble:
                    ensembleTag = tag.Value;
                    break;

                case MPDTags.TrackTags.Duration:
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

        if (!string.IsNullOrWhiteSpace(ensembleTag)) AddArtist(ensembleTag, ArtistRoles.Ensemble);

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
                var artistInfo = new TrackArtistInfo
                {
                    Roles = roles,
                    Artist = new ArtistInfo
                    {
                        Name = artistName
                    }
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
                case MPDTags.QueueTags.Position:
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

        foreach (var tag in tags)
        {
            switch (tag.Name.ToLowerInvariant())
            {
                case MPDTags.AlbumTags.Track:
                    trackNumberTag = tag.Value;
                    break;
                
                case MPDTags.AlbumTags.Disc:
                    diskTag = tag.Value;
                    break;
            }
        }
        
        var trackInfo = ParseTrackInformation(tags);
        var isTrackNumberFound = int.TryParse(trackNumberTag, out var trackNumber);

        var albumTrackInfo = new AlbumTrackInfo
        {
            TrackNumber = isTrackNumberFound ? trackNumber : null,
            VolumeName = diskTag,
            Track = trackInfo
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
                case MPDTags.AlbumTags.Album:
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
}