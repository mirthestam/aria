using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.Connection.Commands;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Infrastructure;
using Microsoft.Extensions.Logging;
using MpcNET;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Queue;
using MpcNET.Commands.Reflection;
using PlaylistInfoCommand = Aria.Backends.MPD.Connection.Commands.PlaylistInfoCommand;

namespace Aria.Backends.MPD;

public class Queue(Client client, ITagParser parser, ILogger<Queue> logger) : BaseQueue
{
    public override async Task<IEnumerable<QueueTrackInfo>> GetTracksAsync()
    {
        var (isSuccess, tagPairs) = await client.SendCommandAsync(new PlaylistInfoCommand()).ConfigureAwait(false);
        if (!isSuccess) throw new InvalidOperationException("Failed to get playlist info");
        if (tagPairs == null) throw new InvalidOperationException("No playlist info found");

        var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();

        return parser.ParseQueueTracksInformation(tags);
    }
    
    public override Task EnqueueAsync(Info info, EnqueueAction action)
    {
        return EnqueueAsync([info], action);
    }

    public override async Task EnqueueAsync(IEnumerable<Info> items, EnqueueAction action)
    {
        // in MPD, we enqueue per track. Therefore, lets's expand our items.

        var tracks = new List<TrackInfo>();
        foreach (var info in items)
        {
            switch (info)
            {
                case AlbumTrackInfo albumTrack:
                    tracks.Add(albumTrack.Track);
                    break;
                
                case TrackInfo track:
                    tracks.Add(track);
                    break;

                case AlbumInfo album:
                    tracks.AddRange(album.Tracks.Select(t => t.Track));
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(items), items, null);
            }            
        }
        
        await EnqueueAsync(tracks, action).ConfigureAwait(false);
        
    }

    public override async Task EnqueueAsync(Info info, int index)
    {
        switch (info)
        {
            case TrackInfo track:
                await EnqueueAsync([track], index).ConfigureAwait(false);
                break;

            case AlbumInfo album:
                await EnqueueAsync(album.Tracks.Select(t => t.Track), index).ConfigureAwait(false);
                break;
        }
    }

    public override async Task MoveAsync(Id sourceTrackId, int targetPlaylistIndex)
    {
        try
        {
            var queueTrackId = (QueueTrackId)sourceTrackId;

            // When the target is located after the source in the queue, move it up by one position.
            // MPD seems to handle this by first removing the track, then reinserting it at the new index.
            var tracks = await GetTracksAsync().ConfigureAwait(false);
            var sourceTrack = tracks.FirstOrDefault(t => t.Id == queueTrackId);
            if (sourceTrack == null) throw new InvalidOperationException("Source track not found");
            if (targetPlaylistIndex > sourceTrack.Position) targetPlaylistIndex--;

            var command = new MoveIdCommand(queueTrackId.Value, targetPlaylistIndex);
            await client.SendCommandAsync(command).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to move track");
        }
    }

    public override async Task ClearAsync()
    {
        try
        {
            await client.SendCommandAsync(new ClearCommand()).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to clear queue");
        }
    }

    public override async Task RemoveTrackAsync(Id trackId)
    {
        try
        {
            var queueTrackId = (QueueTrackId)trackId;
            await client.SendCommandAsync(new DeleteIdCommand(queueTrackId.Value)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to remove track");
        }
    }

    public override async Task SetShuffleAsync(bool enabled)
    {
        try
        {
            var command = new RandomCommand(enabled);
            await client.SendCommandAsync(command).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to set shuffle");
        }
    }

    public override async Task SetRepeatAsync(RepeatMode repeatMode)
    {
        try
        {
            bool repeat;
            bool single;

            switch (repeatMode)
            {
                case RepeatMode.Disabled:
                    repeat = false;
                    single = false;
                    break;
                
                case RepeatMode.All:
                    repeat = true;
                    single = false;
                    break;
                
                case RepeatMode.Single:
                    repeat = true;
                    single = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null);
            }

            using var scope = await client.CreateConnectionScopeAsync();
            
            await scope.SendCommandAsync(new RepeatCommand(repeat)).ConfigureAwait(false);
            await scope.SendCommandAsync(new SingleCommand(single)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to set repeat");
        }
    }

    public override async Task SetConsumeAsync(bool enabled)
    {
        try
        {
            var command = new ConsumeCommand(enabled);
            await client.SendCommandAsync(command).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to set consume");
        }
    }

    private async Task EnqueueAsync(IEnumerable<TrackInfo> tracks, int index)
    {
        try
        {
            var commandList = new CommandList();
            foreach (var track in tracks.Reverse())
            {
                commandList.Add(new AddCommand(track.FileName, index));
            }

            await client.SendCommandAsync(commandList).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to play tracks");
        }
    }

    private async Task EnqueueAsync(IEnumerable<TrackInfo> tracks, EnqueueAction action)
    {
        try
        {
            var commandList = new CommandList();


            switch (action)
            {
                case EnqueueAction.Replace:
                    commandList.Add(new ClearCommand());
                    foreach (var track in tracks)
                    {
                        commandList.Add(new AddCommand(track.FileName));
                    }

                    commandList.Add(new PlayCommand(0));
                    break;
                case EnqueueAction.EnqueueNext:
                    foreach (var track in tracks.Reverse())
                    {
                        commandList.Add(new AddCommand(track.FileName, Order.CurrentIndex + 1 ?? 0));
                    }

                    break;

                case EnqueueAction.EnqueueEnd:
                    foreach (var track in tracks)
                    {
                        commandList.Add(new AddCommand(track.FileName));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            await client.SendCommandAsync(commandList).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to play tracks");
        }
    }

    public async Task UpdateFromStatusAsync(MpdStatus e)
    {
        // We received new information from MPD. Get all relevant information for this playlist.
        var flags = QueueStateChangedFlags.None;

        // Consume
        if (Consume.Enabled != e.Consume || !Consume.Supported)
        {
            Consume = new ConsumeSettings
            {
                Enabled = e.Consume,
                Supported = true
            };
            flags |= QueueStateChangedFlags.Consume;
        }

        // Shuffle
        if (Shuffle.Enabled != e.Random || !Shuffle.Supported)
        {
            Shuffle = new ShuffleSettings
            {
                Enabled = e.Random,
                Supported = true
            };
            flags |= QueueStateChangedFlags.Shuffle;
        }

        // Repeat
        var newRepeatMode = RepeatMode.Disabled;
        if (e is { Repeat: true, Single: true }) newRepeatMode = RepeatMode.Single;
        newRepeatMode = e.Repeat switch
        {
            true when !e.Single => RepeatMode.All,
            false => RepeatMode.Disabled,
            _ => newRepeatMode
        };

        if (newRepeatMode != Repeat.Mode || !Repeat.Supported)
        {
            Repeat = new RepeatSettings
            {
                Mode = newRepeatMode,
                Supported = true
            };
            flags |= QueueStateChangedFlags.Repeat;
        }

        // Playlist 
        var newPlaylistId = new QueueId(e.Playlist);

        if (Id is not QueueId)
        {
            // This is the first time we are processing, this instance.
            // Therefore, just force an update of the playbackOrder.
            // We will NOT update the ID; as this would case the player to
            // reload the playlist it had loaded at the connection.
            Id = newPlaylistId;
            Length = e.PlaylistLength;
            flags |= QueueStateChangedFlags.Id;
            flags |= QueueStateChangedFlags.PlaybackOrder;
        }

        // Skip comparison unless a playlist ID is present to prevent loading the playlist unnecessarily from the app.
        else if (Id is not QueueId oldId || newPlaylistId.Value != oldId.Value)
        {
            flags |= QueueStateChangedFlags.Id;
            flags |= QueueStateChangedFlags.PlaybackOrder;

            // // The playback order changed implicitly, even though the current track may still be at the same index.
            // // This will trigger an order change
            // Order = PlaybackOrder.Default;

            Id = newPlaylistId;
            Length = e.PlaylistLength;
        }

        // Order
        if (Order.CurrentIndex != e.Song)
        {
            Order = new PlaybackOrder
            {
                CurrentIndex = e.Song,
                HasNext = e.NextSongId > 0
            };
            flags |= QueueStateChangedFlags.PlaybackOrder;
        }

        if (flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            var (isSuccess, keyValuePairs) =
                await client.SendCommandAsync(new GetCurrentTrackInfoCommand()).ConfigureAwait(false);
            if (!isSuccess) throw new InvalidOperationException("Failed to get current track info");
            if (keyValuePairs == null) throw new InvalidOperationException("No current track info found");

            var tagPairs = keyValuePairs.ToList();

            var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();
            if (tagPairs.Count == 0)
            {
                CurrentTrack = null;
            }
            else
            {
                var trackInfo = parser.ParseQueueTrackInformation(tags);
                if (trackInfo.Track.FileName != null)
                {
                    // This logic is duplicate with logic in the library.
                    trackInfo = trackInfo with
                    {
                        Track = trackInfo.Track with
                        {
                            Assets =
                            [
                                new AssetInfo
                                {
                                    Id = new AssetId(trackInfo.Track.FileName),
                                    Type = AssetType.FrontCover
                                }
                            ]
                        }
                    };
                }

                CurrentTrack = trackInfo;
            }
        }

        if (flags != QueueStateChangedFlags.None)
        {
            OnStateChanged(flags);
        }
    }
}