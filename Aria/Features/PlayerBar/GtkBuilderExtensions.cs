using Aria.Hosting;

namespace Aria.Features.PlayerBar;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithPlayerBarGTypes()
        {
            builder.WithGType<PlayerBar>();
        }
    }
}