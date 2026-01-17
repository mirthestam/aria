using Aria.Core.Library;
using Aria.Core.Playlist;
using Aria.Infrastructure;

namespace Aria.Backends.Stub;

public class Queue : BaseQueue
{
    public override PlaybackOrder Order => new PlaybackOrder
    {
        CurrentIndex = 0,
        HasNext = true
    };

    public override int Length => 2;

    public override Task<IEnumerable<SongInfo>> GetSongsAsync()
    {
        var songs = new List<SongInfo>{
            new()
            {
                Id = new StubId(1),
                Duration = TimeSpan.FromSeconds(123),
                Title = "Debugging templates",
                CreditsInfo = new SongCreditsInfo
                {
                    Artists =
                    [
                        new()
                        {
                            Artist = new()
                            {
                                Id = new StubId(3),
                                Name = "The Gircorries"
                            },
                            Roles = ArtistRoles.Performer
                        },
                        new()
                        {
                            Artist = new()
                            {
                                Id = new StubId(4),
                                Name = "The Garbage Collector"
                            },
                            Roles = ArtistRoles.Conductor
                        }
                    ],
                    AlbumArtists =
                    [
                        new()
                        {
                            Id = new StubId(3),
                            Name = "The Gircorries"
                        }
                    ]
                },
                ReleaseDate = new DateTime(2026,01,02),
            },
            new()
            {
                Id = new StubId(2),
                Duration = TimeSpan.FromSeconds(123),
                Title = "Intermediate Language talk",
                CreditsInfo = new SongCreditsInfo
                {
                    Artists =
                    [
                        new()
                        {
                            Artist = new()
                            {
                                Id = new StubId(3),
                                Name = "The Gircorries"
                            },
                            Roles = ArtistRoles.Performer
                        },
                        new()
                        {
                            Artist = new()
                            {
                                Id = new StubId(5),
                                Name = "The Compiler"
                            },
                            Roles = ArtistRoles.Conductor
                        }
                    ],
                    AlbumArtists =
                    [
                        new()
                        {
                            Id = new StubId(3),
                            Name = "The Gircorries"
                        }
                    ]
                },
                ReleaseDate = new DateTime(2026,01,02),
            }
        };
        
        return Task.FromResult<IEnumerable<SongInfo>>(songs);
    }

    public override SongInfo? CurrentSong => new()
    {
        Id = new StubId(1),
        Duration = TimeSpan.FromSeconds(123),
        Title = "Debugging templates",
        CreditsInfo = new SongCreditsInfo
        {
            Artists = new List<SongArtistInfo>
            {
                new()
                {
                    Artist = new ArtistInfo
                    {
                        Id = new StubId(3),
                        Name = "The Gircorries"
                    },
                    Roles = ArtistRoles.Performer
                },
                new()
                {
                    Artist = new ArtistInfo
                    {
                        Id = new StubId(1),
                        Name = "The Garbage Collector"
                    },
                    Roles = ArtistRoles.Conductor
                }
            },
            AlbumArtists = new List<ArtistInfo>
            {
                new()
                {
                    Id = new StubId(3),
                    Name = "The Gircorries"
                }
            }
        },
        Work = null,
        ReleaseDate = null,
        FileName = null
    };
}