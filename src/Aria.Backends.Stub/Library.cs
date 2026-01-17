using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;

namespace Aria.Backends.Stub;

public class Library : BaseLibrary
{
    public static ArtistInfo Gircorries => new() { Id = new StubId(3), Name = "The gircorries", Roles = ArtistRoles.Performer };
    public static ArtistInfo GarbageCollector => new() { Id = new StubId(1), Name = "The Garbage Collector", Roles = ArtistRoles.Conductor };
    public static ArtistInfo Compiler => new() { Id = new StubId(2), Name = "Compiler", Roles = ArtistRoles.Conductor };

    public static SongInfo DebuggingSong => new()
    {
        Id = new StubId(1),
        Duration = TimeSpan.FromSeconds(60),
        Title = "Debugging templates",
        CreditsInfo = new SongCreditsInfo
        {
            Artists = [
                new() { Artist = Gircorries, Roles = ArtistRoles.Performer },
                new() { Artist = GarbageCollector, Roles = ArtistRoles.Conductor }
            ],
            AlbumArtists = [Gircorries]
        },
        ReleaseDate = new DateTime(2026, 01, 02)
    };

    public static SongInfo ILTalkSong => new()
    {
        Id = new StubId(2),
        Duration = TimeSpan.FromSeconds(75),
        Title = "Intermediate Language talk",
        CreditsInfo = new SongCreditsInfo
        {
            Artists = [
                new() { Artist = Gircorries, Roles = ArtistRoles.Performer },
                new() { Artist = Compiler, Roles = ArtistRoles.Conductor }
            ],
            AlbumArtists = [Gircorries]
        },
        ReleaseDate = new DateTime(2026, 01, 02)
    };

    public override async Task<ArtistInfo?> GetArtist(Id artistId, CancellationToken cancellationToken = default)
    {
        var artists = await GetArtists(cancellationToken).ConfigureAwait(false);
        return artists.FirstOrDefault(artist => artist.Id == artistId);
    }

    public override async Task<IEnumerable<ArtistInfo>> GetArtists(CancellationToken cancellationToken = default)
    {
        await Task.Delay(BackendConnection.Delay, cancellationToken).ConfigureAwait(false);
        return [GarbageCollector, Compiler, Gircorries];
    }

    public override async Task<IEnumerable<AlbumInfo>> GetAlbums(CancellationToken cancellationToken = default)
    {
        await Task.Delay(BackendConnection.Delay, cancellationToken).ConfigureAwait(false);
        return [
            new AlbumInfo
            {
                Title = "To C# Chronicles",
                ReleaseDate = new DateTime(2026, 01, 02),
                Id = new StubId(1),
                Assets = [new() { Id = new StubId(1), Type = AssetType.FrontCover }],
                Songs = [DebuggingSong, ILTalkSong],
                CreditsInfo = new AlbumCreditsInfo
                {
                    Artists = [
                        new() { Artist = Gircorries, Roles = ArtistRoles.Performer },
                        new() { Artist = GarbageCollector, Roles = ArtistRoles.Conductor }
                    ],
                    AlbumArtists = [Gircorries]
                }
            }
        ];
    }

    public override Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId, CancellationToken cancellationToken = default) => GetAlbums(cancellationToken);
}

public class StubId(int id) : Id.TypedId<int>(id, "STUB")
{
}