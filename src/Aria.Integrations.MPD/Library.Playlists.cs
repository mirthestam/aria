using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using MpcNET.Commands.Playlist;
using ListPlaylistInfoCommand = Aria.Backends.MPD.Connection.Commands.ListPlaylistInfoCommand;

namespace Aria.Backends.MPD;

public partial class Library
{
    private readonly PlaylistParser _playlistParser = new(tagParser);
    
    // Playlists
    public override async Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.SendCommandAsync(new ListPlaylistsCommand(), token: cancellationToken);
        if (!response.IsSuccess) return [];
        
        return response.Content!.Select(playlist => new PlaylistInfo
        {
            Name = playlist.Name, 
            LastModified = playlist.LastModified, 
            Id = new PlaylistId(playlist.Name)
        }).ToList();
    }
    
    public override async Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default)
    {
        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken);
        
        var typedPlaylistId = (PlaylistId)playlistId;
        
        var response = await scope.SendCommandAsync(new ListPlaylistInfoCommand(typedPlaylistId.Value));
        if (!response.IsSuccess) return null;
        var tags = response.Content!.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();

        // I need 2 commands
        // the META and the files command
        
        var playlists = await GetPlaylistsAsync(cancellationToken).ConfigureAwait(false);
        var playlist = playlists.FirstOrDefault(x => x.Id == typedPlaylistId);
        if (playlist == null) return null;
        
        var tracks = _playlistParser.GetPlaylist(tags);
        
        return playlist with
        {
            Tracks = tracks.ToList()
        };
    }    
}