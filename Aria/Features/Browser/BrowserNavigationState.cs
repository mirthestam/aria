using Aria.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aria.Features.Browser;

public partial class BrowserNavigationState : ObservableObject
{
    [ObservableProperty] private Id? _selectedArtistId;
}