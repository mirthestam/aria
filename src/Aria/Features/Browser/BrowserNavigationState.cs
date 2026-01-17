using Aria.Core;
using Aria.Core.Extraction;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aria.Features.Browser;

public partial class BrowserNavigationState : ObservableObject
{
    /*
     * NAVIGATION STRATEGY:
     *
     * 1. Messages (Show...Message):
     *    The "Intent". Use these to request a navigation change from anywhere in the app.
     *
     * 2. NavigationState (SelectedArtistId):
     *    The "Global Context". Keeps the sidebar and background pages in sync with
     *    the currently active artist. It is the 'Source of Truth' for the browser's base state.
     *
     * 3. Page Parameters (AlbumId):
     *    The "UI Stack". Since albums are pushed onto the Adw.NavigationView stack,
     *    we pass the ID directly to the page. This allows the native UI to handle
     *    the history and 'Back' button without cluttering the global state.
     */    
    
    [ObservableProperty] private Id? _selectedArtistId;
}