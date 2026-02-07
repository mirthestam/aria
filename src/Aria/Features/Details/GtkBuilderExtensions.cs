using Aria.Features.Player.Queue;
using Aria.Hosting;

namespace Aria.Features.Details;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithDetailsGTypes()
        {
            builder.WithGType<TrackDetailsDialog>();
        }
    }
}