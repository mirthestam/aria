using Aria.Hosting;

namespace Aria.Features.Shared;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithSharedGTypes()
        {
            builder.WithGType<PlayButton>();
        }
    }
}