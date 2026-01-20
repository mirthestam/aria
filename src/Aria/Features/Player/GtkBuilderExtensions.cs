using Aria.Hosting;

namespace Aria.Features.Player;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithPlayerGTypes()
        {
            builder.WithGType<MediaControls>();
            builder.WithGType<PlaybackControls>();
            builder.WithGType<Player>();
            builder.WithGType<Playlist.Playlist>();
            builder.WithGType<Playlist.TrackListItem>();
        }
    }
}