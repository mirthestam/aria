using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure;

namespace Aria.Backends.Stub;

public class Library : BaseLibrary
{
    public override Task<IEnumerable<ArtistInfo>> GetArtists()
    {
        return Task.FromResult<IEnumerable<ArtistInfo>>(new List<ArtistInfo>
        {
            new()
            {
                Id = new StubId(1),
                Name = "The Garbage Collector",
                Roles = ArtistRoles.Conductor
            },
            new()
            {
                Id = new StubId(2),
                Name = "Compiler",
                Roles = ArtistRoles.Conductor
            },            
            new()
            {
                Id = new StubId(3),
                Name = "The gircorries",
                Roles = ArtistRoles.Performer
            },            
        });
    }

    public override Task<IEnumerable<AlbumInfo>> GetAlbums()
    {
        // TODO: I noticed, when no front cover ID, the UI does not load the default
        
        return Task.FromResult<IEnumerable<AlbumInfo>>(new List<AlbumInfo>
        {
            new()
            {
                Title = "To C# Chronicles",
                ReleaseDate = new DateTime(2026,01,02),
                Id =new StubId(1),
                Assets = new List<AssetInfo>
                {
                    new()
                    {
                        Id = new StubId(1),
                        Type = AssetType.FrontCover
                    }
                },
                Songs = new List<SongInfo>
                {
                    new()
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
                    },
                    new()
                    {
                        Id = new StubId(1),
                        Duration = TimeSpan.FromSeconds(123),
                        Title = "Intermediate Language talk",
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
                                        Id = new StubId(2),
                                        Name = "The Compiler"
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
                    }                    
                },
                CreditsInfo = new AlbumCreditsInfo
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
                }
            }            
        });
    }

    public override Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId) => GetAlbums();
}

public class StubId(int id) : Id.TypedId<int>(id, "STUB")
{
}