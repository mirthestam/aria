using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Playlist;
using Aria.Infrastructure.Tagging;
using CommunityToolkit.Mvvm.Messaging;
using MpcNET;
using MpcNET.Commands.Queue;

namespace Aria.MusicServers.MPD;

public class Playlist(Session session, IMessenger messenger, ITagParser parser) : IPlaylist
{
    // This file is a proof of concept.The playlist has not been implemented yet.
    
    public Id Id { get; private set; }
    public int Length { get; private set; }
    public PlaybackOrder Order { get; private set; }
    public ShuffleSettings Shuffle { get; private set; }
    public RepeatSettings Repeat { get; private set; }
    public ConsumeSettings Consume { get; private set; }

    public Task SetShuffleAsync(bool enabled)
    {
        throw new NotImplementedException();
    }

    public Task SetRepeatAsync(bool enabled)
    {
        throw new NotImplementedException();
    }

    public Task SetConsumeAsync(bool enabled)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<SongInfo>> GetSongsAsync()
    {
        var (isSuccess, tagPairs) = await session.SendCommandAsync(new Aria.MusicServers.MPD.Commands.PlaylistInfoCommand());
        if (!isSuccess)  throw new InvalidOperationException("Failed to get playlist info");
        if (tagPairs == null) throw new InvalidOperationException("No playlist info found");

        var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();
        return parser.ParseSongsInformation(tags);
    }
    
    public void UpdateFromStatus(MpdStatus e)
    {
        var flags = PlaylistStateChangedFlags.None;

        var currentIndex = e.SongId;
        var nextSongId= e.NextSongId;
        
        var newOrder = new PlaybackOrder(currentIndex, nextSongId);
        if (Order != newOrder)
        {
            Order = newOrder;
            flags |= PlaylistStateChangedFlags.PlaybackOrder;
        }
        
        var newConsumeSettings = new ConsumeSettings(true, e.Consume);
        if (Consume != newConsumeSettings)
        {
            flags |= PlaylistStateChangedFlags.Consume;
            Consume = newConsumeSettings;
        }

        var newShuffleSettings = new ShuffleSettings(true, e.Random);
        if (Shuffle != newShuffleSettings)
        {
            flags |= PlaylistStateChangedFlags.Shuffle;
            Shuffle = newShuffleSettings;
        }

        var newRepeatSettings = new RepeatSettings(true, e.Repeat, e.Single);
        if (Repeat != newRepeatSettings)
        {
            flags |= PlaylistStateChangedFlags.Repeat;
            Repeat = newRepeatSettings;
        }

        var newPlaylistId = new PlaylistId(e.Playlist);
        if (Id is not PlaylistId oldId || newPlaylistId.Value != oldId.Value)
        {
            flags |= PlaylistStateChangedFlags.Id;
            Id = newPlaylistId;
        }

        if (flags  != PlaylistStateChangedFlags.None)
            messenger.Send(new PlaylistChangedMessage(flags));
    }
}