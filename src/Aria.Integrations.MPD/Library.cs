using System.Diagnostics;
using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using Microsoft.Extensions.Logging;
using MpcNET;
using MpcNET.Commands.Database;
using MpcNET.Tags;
using FindCommand = Aria.MusicServers.MPD.Commands.FindCommand;

namespace Aria.MusicServers.MPD;

public class Library(Session session, ITagParser tagParser, ILogger<Library> logger) : BaseLibrary
{
    private readonly AlbumsParser _albumsParser = new(tagParser);

    public override async Task<IEnumerable<ArtistInfo>> GetArtists()
    {
        var artistMap = new Dictionary<Id, ArtistInfo>();

        using var scope = await session.CreateConnectionScopeAsync();
        await FetchAndAdd(new ListCommand(MpdTags.Artist), ArtistRoles.Performer, scope);
        await FetchAndAdd(new ListCommand(MpdTags.Composer), ArtistRoles.Composer, scope);
        await FetchAndAdd(new ListCommand(MpdTags.Performer), ArtistRoles.Performer, scope);
        await FetchAndAdd(new ListCommand(ExtraMpdTags.Conductor), ArtistRoles.Conductor, scope);
        await FetchAndAdd(new ListCommand(ExtraMpdTags.Ensemble), ArtistRoles.Ensemble, scope);

        return artistMap.Values;

        async Task FetchAndAdd(IMpcCommand<IEnumerable<string>> command, ArtistRoles role,
            ConnectionScope connectionScope)
        {
            var (isSuccess, names) = await connectionScope.SendCommandAsync(command);
            if (isSuccess && names != null)
            {
                foreach (var name in names) AddOrUpdate(name, role);
            }
        }

        void AddOrUpdate(string artistName, ArtistRoles roles)
        {
            var id = new ArtistId(artistName);
            if (string.IsNullOrWhiteSpace(artistName)) return;

            if (artistMap.TryGetValue(id, out var existingArtist))
            {
                artistMap[id] = existingArtist with
                {
                    Roles = existingArtist.Roles | roles
                };
            }
            else
            {
                var artistInfo = new ArtistInfo
                {
                    Id = id,
                    Name = artistName,
                    Roles = roles
                };
                artistMap[id] = artistInfo;
            }
        }
    }

    public override async Task<IEnumerable<AlbumInfo>> GetAlbums()
    {
        var artists = (await GetArtists()).ToList();
        var allTags = new List<Tag>();

        using (var scope = await session.CreateConnectionScopeAsync())
        {
            var tasks = artists
                .Select(artist => scope.SendCommandAsync(new FindCommand(MpdTags.AlbumArtist, artist.Name))).ToList();

            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                if (response is { IsSuccess: true, Content: not null })
                {
                    allTags.AddRange(response.Content.Select(x => new Tag(x.Key, x.Value)));
                }
            }
        }

        return _albumsParser.GetAlbums(allTags).DistinctBy(album => album.Id);
    }

    public override async Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId)
    {
        var mpdArtistId = (ArtistId)artistId;

        using var scope = await session.CreateConnectionScopeAsync();
        
        var tasks = new[]
        {
            scope.SendCommandAsync(new FindCommand(MpdTags.AlbumArtist, mpdArtistId.Value)),
            scope.SendCommandAsync(new FindCommand(MpdTags.Composer, mpdArtistId.Value)),
            scope.SendCommandAsync(new FindCommand(ExtraMpdTags.Conductor, mpdArtistId.Value)),
            scope.SendCommandAsync(new FindCommand(ExtraMpdTags.Ensemble, mpdArtistId.Value)),
            scope.SendCommandAsync(new FindCommand(MpdTags.Performer, mpdArtistId.Value))
        };

        var results = await Task.WhenAll(tasks);
        
        var allTags = results
            .Where(r => r is { IsSuccess: true, Content: not null })
            .SelectMany(r => r.Content!)
            .Select(pair => new Tag(pair.Key, pair.Value))
            .ToList();

        return _albumsParser.GetAlbums(allTags);
    }

    public override async Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token)
    {
        if (resourceId == Id.Empty) return await GetDefaultAlbumResourceStreamAsync();

        var albumArtId = (AssetId)resourceId;
        var fileName = albumArtId.Value;

        using var scope = await session.CreateConnectionScopeAsync(token);


        // Try to find the cover from directory the song resides in by looking for a file called cover.png, cover.jpg, or cover.webp.
        try
        {
            long totalBinarySize = 9999;
            long currentSize = 0;
            var data = new List<byte>();

            do
            {
                var (isSuccess, content) = await scope.SendCommandAsync(new AlbumArtCommand(fileName, currentSize));
                if (!isSuccess) break;
                if (content == null) break;

                if (content.Binary == 0) break;

                totalBinarySize = content.Size;
                currentSize += content.Binary;
                data.AddRange(content.Data);
            } while (currentSize < totalBinarySize && !token.IsCancellationRequested);

            if (data.Count > 0)
            {
                return new MemoryStream(data.ToArray());
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get album art from MPD");
        }

        // Try to find the art by reading embedded pictures from binary tags (e.g. ID3v2’s APIC tag).
        try
        {
            long totalBinarySize = 9999;
            long currentSize = 0;
            var data = new List<byte>();

            do
            {
                var (isSuccess, content) = await scope.SendCommandAsync(new ReadPictureCommand(fileName, currentSize));
                if (!isSuccess) break;
                if (content == null) break;
                if (content.Binary == 0) break;

                totalBinarySize = content.Size;
                currentSize += content.Binary;
                data.AddRange(content.Data);
            } while (currentSize < totalBinarySize && !token.IsCancellationRequested);

            if (data.Count > 0)
            {
                return new MemoryStream(data.ToArray());
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get album art from MPD");
        }

        // No album art found. Just return the default album art.
        return await GetDefaultAlbumResourceStreamAsync();
    }
}