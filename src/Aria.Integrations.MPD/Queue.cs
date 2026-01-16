using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Playlist;
using Aria.Infrastructure.Tagging;
using Aria.MusicServers.MPD.Commands;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using MpcNET;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Queue;
using MpcNET.Commands.Reflection;
using MpcNET.Types;

namespace Aria.MusicServers.MPD;

public class Queue(Session session, IMessenger messenger, ITagParser parser, ILogger<Queue> logger) : IQueue
{
    public Id Id { get; private set; } = Id.Empty;
    public int Length { get; private set; }
    public PlaybackOrder Order { get; private set; }
    public ShuffleSettings Shuffle { get; private set; }
    public RepeatSettings Repeat { get; private set; }
    public ConsumeSettings Consume { get; private set; }

    public Task SetShuffleAsync(bool enabled) => throw new NotImplementedException();
    public Task SetRepeatAsync(bool enabled) => throw new NotImplementedException();
    public Task SetConsumeAsync(bool enabled) => throw new NotImplementedException();

    public async Task<IEnumerable<SongInfo>> GetSongsAsync()
    {
        var (isSuccess, tagPairs) = await session.SendCommandAsync(new Commands.PlaylistInfoCommand());
        if (!isSuccess) throw new InvalidOperationException("Failed to get playlist info");
        if (tagPairs == null) throw new InvalidOperationException("No playlist info found");

        var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();
        return parser.ParseSongsInformation(tags);
    }

    public SongInfo? CurrentSong { get; private set; }
    public async Task PlayAsync(int index)
    {
        await session.SendCommandAsync(new PlayCommand(index));
    }

    public async Task PlayAlbum(AlbumInfo album)
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
        
            await session.SendCommandAsync(commandList);

        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to play album");
        }
    }

    public async Task UpdateFromStatusAsync(MpdStatus e)
    {
        // We received new information from MPD. Get all relevant information for this playlist.
        var flags = QueueStateChangedFlags.None;

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
        if (Id is not QueueId oldId || newPlaylistId.Value != oldId.Value)
        {
            flags |= QueueStateChangedFlags.Id;
            Id = newPlaylistId;
            Length = e.PlaylistLength;
        }

        if (flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            var (isSuccess, keyValuePairs) = await session.SendCommandAsync(new GetCurrentSongInfoCommand());
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

        if (flags != QueueStateChangedFlags.None) messenger.Send(new QueueChangedMessage(flags));
    }
}