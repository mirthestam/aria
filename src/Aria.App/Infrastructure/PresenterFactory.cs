using Aria.Features.Browser.Album;
using Aria.Features.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Aria.App.Infrastructure;

public class PresenterFactory(IServiceProvider serviceProvider) : IPresenterFactory
{
    public TPresenter Create<TPresenter>() where TPresenter : IPresenter
    {
        return serviceProvider.GetRequiredService<TPresenter>();
    }
}