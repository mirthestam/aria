namespace Aria.Core.Library;

/// <summary>
/// Convenience methods to filter for common 'roles' on the artists
/// </summary>
public static class SongCreditInfoExtensions
{
    extension(SongCreditsInfo info)
    {
        public IEnumerable<SongArtistInfo> Composers => info.Artists.Where(x => x.Roles.HasFlag(ArtistRoles.Composer));
        
        public IEnumerable<SongArtistInfo> Conductors => info.Artists.Where(x => x.Roles.HasFlag(ArtistRoles.Conductor));
        
        public IEnumerable<SongArtistInfo> OtherArtists => info.Artists.Where(x =>
            !x.Roles.HasFlag(ArtistRoles.Composer) &&
            !x.Roles.HasFlag(ArtistRoles.Conductor));        
    }
}