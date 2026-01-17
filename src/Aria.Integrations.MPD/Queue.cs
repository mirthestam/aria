using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.Connection.Commands;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using MpcNET;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Queue;
using MpcNET.Commands.Reflection;
using PlaylistInfoCommand = Aria.Backends.MPD.Connection.Commands.PlaylistInfoCommand;

namespace Aria.Backends.MPD;

public class Queue(Client client, ITagParser parser, ILogger<Queue> logger) : BaseQueue
{
    public override async Task<IEnumerable<SongInfo>> GetSongsAsync()
    {
        var (isSuccess, tagPairs) = await client.SendCommandAsync(new PlaylistInfoCommand()).ConfigureAwait(false);
        if (!isSuccess) throw new InvalidOperationException("Failed to get playlist info");
        if (tagPairs == null) throw new InvalidOperationException("No playlist info found");

        var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();
        return parser.ParseSongsInformation(tags);
    }
    
    public override async Task PlayAsync(int index)
    {
        await client.SendCommandAsync(new PlayCommand(index)).ConfigureAwait(false);
    }

    public override async Task PlayAlbum(AlbumInfo album)
    {
        try
        {
            // Using a command list here as we want MPD to process these commands sequentially.
            var commandList = new CommandList();
            commandList.Add(new ClearCommand());
            foreach (var song in album.Songs)
            {
                commandList.Add(new AddCommand(song.FileName));            
            }

            commandList.Add(new PlayCommand(0));        
        
            await client.SendCommandAsync(commandList).ConfigureAwait(false);

        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to play album");
        }
    }

    public override async Task EnqueueAlbum(AlbumInfo album)
    {
        try
        {
            // Using a command list here as we want MPD to process these commands sequentially.
            var commandList = new CommandList();
            foreach (var song in album.Songs)
            {
                commandList.Add(new AddCommand(song.FileName));            
            }
            
            await client.SendCommandAsync(commandList).ConfigureAwait(false);

        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to enqueue album");
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
        if (Repeat.Enabled != e.Repeat || Repeat.Single != e.Single || !Repeat.Supported)
        {
            Repeat = new RepeatSettings
            {
                Enabled = e.Repeat,
                Single = e.Single,
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
            flags |= QueueStateChangedFlags.PlaybackOrder;
        }
        
        // Skip comparison unless a playlist ID is present to prevent loading the playlist unnecessarily from the app.
        else if (Id is not QueueId oldId || newPlaylistId.Value != oldId.Value)
        {
            flags |= QueueStateChangedFlags.Id;
            flags |= QueueStateChangedFlags.PlaybackOrder;
            
            // // The playback order changed implicitly, even though the current song may still be at the same index.
            // // This will trigger an order chaange
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
            var (isSuccess, keyValuePairs) = await client.SendCommandAsync(new GetCurrentSongInfoCommand()).ConfigureAwait(false);
            if (!isSuccess) throw new InvalidOperationException("Failed to get current song info");
            if (keyValuePairs == null) throw new InvalidOperationException("No current song info found");

            var tagPairs = keyValuePairs.ToList();

            var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();
            if (tagPairs.Count == 0)
            {
                CurrentSong = null;
            }
            else
            {
                var songInfo = parser.ParseSongInformation(tags);
                if (songInfo.FileName != null)
                {
                    // This logic is duplicate with logic in the library.
                    songInfo = songInfo with
                    {
                        Assets =
                        [
                            new AssetInfo
                            {
                                Id = new AssetId(songInfo.FileName),
                                Type = AssetType.FrontCover
                            }
                        ]
                    };
                }

                CurrentSong = songInfo;
            }
        }

        if (flags != QueueStateChangedFlags.None)
        {
            OnStateChanged(flags);
        }
    }
}