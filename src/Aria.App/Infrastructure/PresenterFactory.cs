using Aria.Features.Browser;
using Aria.Features.Browser.Album;
using Microsoft.Extensions.DependencyInjection;

namespace Aria.App.Infrastructure;

public class PresenterFactory(IServiceProvider serviceProvider) : IAlbumPagePresenterFactory
{
    public AlbumPagePresenter Create()
    {
        return serviceProvider.GetRequiredService<AlbumPagePresenter>();
    }
}