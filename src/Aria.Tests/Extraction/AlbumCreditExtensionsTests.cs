using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;

namespace Aria.Tests;


// TODO: Tests for extra data parser `lang lang (piano)`
public class StringId(string value) : Id.TypedId<string>(value, "ID")
{
}

// TODO: Rewrite for the new helper tool
// public class AlbumCreditExtensionsTests
// {
//     [Fact]
//     void Foo()
//     {
//         // Arrange
//
//         var beethoven = new ArtistInfo
//             { Id = new StringId("beethoven"), Name = "Beethoven", Roles = ArtistRoles.Composer };
//         var karajan = new ArtistInfo { Id = new StringId("karajan"), Name = "Karajan", Roles = ArtistRoles.Conductor };
//         var berliner = new ArtistInfo
//             { Id = new StringId("berliner"), Name = "Berliner", Roles = ArtistRoles.Ensemble };
//         var operasinger = new ArtistInfo { Id = new StringId("itzak"), Name = "Itzak", Roles = ArtistRoles.Performer };
//
//         var album = new AlbumInfo
//         {
//             Id = new StringId("album"),
//             Title = "Beethovens 9th",
//             CreditsInfo = new AlbumCreditsInfo
//             {
//                 AlbumArtists = new List<ArtistInfo> { beethoven, karajan },
//                 Artists = new List<TrackArtistInfo>
//                 {
//                     new() { Artist = beethoven, Roles = ArtistRoles.Composer },
//                     new() { Artist = karajan, Roles = ArtistRoles.Conductor },
//                     new() { Artist = berliner, Roles = ArtistRoles.Ensemble },
//                     new() { Artist = operasinger, Roles = ArtistRoles.Performer }
//                 }
//             },
//             Tracks = new List<AlbumTrackInfo>
//             {
//                 new()
//                 {
//                     Track = new TrackInfo
//                     {
//                         Id = new StringId("track1"),
//                         Duration = TimeSpan.FromSeconds(10),
//                         Title = "First movement",
//                         CreditsInfo = new TrackCreditsInfo
//                         {
//                             Artists = new List<TrackArtistInfo>
//                             {
//                                 new() { Artist = beethoven, Roles = ArtistRoles.Composer },
//                                 new() { Artist = karajan, Roles = ArtistRoles.Conductor },
//                                 new() { Artist = berliner, Roles = ArtistRoles.Ensemble }
//                             },
//                             AlbumArtists = new List<ArtistInfo> { beethoven, karajan }
//                         },
//                         Work = null,
//                         ReleaseDate = null,
//                         FileName = null,
//                     },
//                     TrackNumber = 1,
//                     VolumeName = null
//                 },
//                 new()
//                 {
//                     Track = new TrackInfo
//                     {
//                         Id = new StringId("track1"),
//                         Duration = TimeSpan.FromSeconds(10),
//                         Title = "Last movement (Ode to Joy)",
//                         CreditsInfo = new TrackCreditsInfo
//                         {
//                             Artists = new List<TrackArtistInfo>
//                             {
//                                 new() { Artist = beethoven, Roles = ArtistRoles.Composer },
//                                 new() { Artist = karajan, Roles = ArtistRoles.Conductor },
//                                 new() { Artist = berliner, Roles = ArtistRoles.Ensemble },
//                                 new() { Artist = operasinger, Roles = ArtistRoles.Performer }
//                             },
//                             AlbumArtists = new List<ArtistInfo> { beethoven, karajan }
//                         },
//                         Work = null,
//                         ReleaseDate = null,
//                         FileName = null
//                     },
//                     TrackNumber = 2,
//                     VolumeName = null
//                 }
//             },
//             ReleaseDate = DateTime.Now
//         };
//
//         // Act
//         var sharedArtists = album.GetSharedArtists().Select(ta => ta.Artist).ToList();
//         var sharedGuestArtists = album.GetSharedGuestArtists().Select(ta => ta.Artist).ToList();
//         var uniqueArtists = album.GetUniqueSongArtists(album.Tracks[^1].Track).Select(ta => ta.Artist).ToList();
//
//         // Assert
//
//         // Beethoven was featured on every track, but also an album artist. So he should not be a common GUEST artist.
//         Assert.Contains(beethoven, sharedArtists);
//         Assert.DoesNotContain(beethoven, sharedGuestArtists);
//         Assert.DoesNotContain(beethoven, uniqueArtists);
//
//         // The berliner was featured on every track, but not titled as album artist, so it should come out as common artists
//         Assert.Contains(berliner, sharedArtists);
//         Assert.Contains(berliner, sharedGuestArtists);
//         Assert.DoesNotContain(berliner, uniqueArtists);
//
//         // The opera singer was only featured on the last track, so he should not be a common artist.
//         Assert.DoesNotContain(operasinger, sharedArtists);
//         Assert.DoesNotContain(operasinger, sharedGuestArtists);
//         Assert.Contains(operasinger, uniqueArtists);
//     }
// }