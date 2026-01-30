using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;
using Microsoft.Extensions.Logging;
using MpcNET;
using MpcNET.Commands.Database;
using MpcNET.Tags;
using MpcNET.Types;
using MpcNET.Types.Filters;
using FindCommand = Aria.Backends.MPD.Connection.Commands.FindCommand;
using SearchCommand = Aria.Backends.MPD.Connection.Commands.SearchCommand;

namespace Aria.Backends.MPD;

public class Library(Client client, ITagParser tagParser, ILogger<Library> logger) : BaseLibrary
{
    private readonly AlbumsParser _albumsParser = new(tagParser);

    public void ServerUpdated()
    {
        OnUpdated();
    }

    public override async Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default)
    {
        return id switch
        {
            TrackId track => await GetTrackAsync(track, cancellationToken).ConfigureAwait(false),
            AlbumId album => await GetAlbumAsync(album, cancellationToken).ConfigureAwait(false),
            ArtistId artist => await GetArtistAsync(artist, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException()
        };
    }

    private async Task<Info?> GetTrackAsync(Id trackId, CancellationToken cancellationToken)
    {
        var fullId = (TrackId)trackId;
        
        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);
        var command = new FindCommand(new FilterFile(fullId.Value, FilterOperator.Equal));
        var response = await scope.SendCommandAsync(command).ConfigureAwait(false);
        if (!response.IsSuccess) return null;
        var tags = response.Content!.Select(x => new Tag(x.Key, x.Value));
        var albums = _albumsParser.GetAlbums(tags.ToList()).ToList();

        if (albums.Count != 1) return null;
        return albums[0].Tracks.Count != 1 ? null : albums[0].Tracks[0].Track;
    }

    public override async Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default)
    {
        var fullId = (AlbumId)albumId;

        var title = fullId.Title;
        var artistNames = fullId.AlbumArtistIds.Select(id => id).Select(id => id.Value);
        var filters = new List<KeyValuePair<ITag, string>>();
        filters.Add(new KeyValuePair<ITag, string>(MpdTags.Album, title));
        filters.AddRange(artistNames.Select(name => new KeyValuePair<ITag, string>(MpdTags.AlbumArtist, name)));

        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        var command = new SearchCommand(filters);
        var response = await scope.SendCommandAsync(command).ConfigureAwait(false);
        if (!response.IsSuccess) return null;

        var tags = response.Content!.Select(x => new Tag(x.Key, x.Value));
        var albums = _albumsParser.GetAlbums(tags.ToList()).ToList();

        switch (albums.Count)
        {
            case 0:
            // unexpected!
            case > 1:
                // Album not found
                return null;
            default:
                return albums[0];
        }
    }

    public override async Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default)
    {
        // It is not a problem that we are using All Artists here.
        // The engine has a burst cache 
        var artists = await GetArtistsAsync(cancellationToken).ConfigureAwait(false);
        return artists.FirstOrDefault(artist => artist.Id == artistId);
    }

    public override async Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default)
    {
        var artists = await GetArtistsAsync(cancellationToken).ConfigureAwait(false);

        if (query.RequiredRoles is { } requiredRoles)
            artists = artists.Where(a => (a.Roles & requiredRoles) != 0); // OR operator effectively
        
        artists = query.Sort switch
        {
            ArtistSort.ByName => artists.OrderBy(a => a.Name),
            _ => artists
        };

        return artists;
    }

    public override async Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default)
    {
        var artistMap = new Dictionary<Id, ArtistInfo>();

        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);
        await FetchAndAdd(new ListCommand(MpdTags.AlbumArtist), ArtistRoles.Main, scope).ConfigureAwait(false);
        await FetchAndAdd(new ListCommand(MpdTags.Artist), ArtistRoles.Performer, scope).ConfigureAwait(false);
        await FetchAndAdd(new ListCommand(MpdTags.Composer), ArtistRoles.Composer, scope).ConfigureAwait(false);
        await FetchAndAdd(new ListCommand(MpdTags.Performer), ArtistRoles.Performer, scope).ConfigureAwait(false);
        await FetchAndAdd(new ListCommand(ExtraMpdTags.Conductor), ArtistRoles.Conductor, scope).ConfigureAwait(false);
        await FetchAndAdd(new ListCommand(ExtraMpdTags.Ensemble), ArtistRoles.Ensemble, scope).ConfigureAwait(false);

        return artistMap.Values;

        async Task FetchAndAdd(IMpcCommand<IEnumerable<string>> command, ArtistRoles role,
            ConnectionScope connectionScope)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (isSuccess, names) = await connectionScope.SendCommandAsync(command).ConfigureAwait(false);
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

    public override async Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        var artists = (await GetArtistsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        var allTags = new List<Tag>();

        using (var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false))
        {
            var tasks = artists
                .Select(artist =>
                    scope.SendCommandAsync(new MPD.Connection.Commands.SearchCommand(MpdTags.AlbumArtist, artist.Name)))
                .ToList();

            var responses = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var response in responses)
            {
                if (response is { IsSuccess: true, Content: not null })
                {
                    allTags.AddRange(response.Content.Select(x => new Tag(x.Key, x.Value)));
                }
            }
        }

        // This is CPU-bound parsing. Since ConfigureAwait(false) was used earlier,
        // this code is assumed to be running on a background thread.
        return _albumsParser.GetAlbums(allTags);
    }

    public override async Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId,
        CancellationToken cancellationToken = default)
    {
        var mpdArtistId = (ArtistId)artistId;

        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        // Either FindCommand or SearchCommand could be used here. SearchCommand is faster because it does not support expressions.
        var tasks = new[]
        {
            scope.SendCommandAsync(new MPD.Connection.Commands.SearchCommand(MpdTags.AlbumArtist, mpdArtistId.Value)),
            scope.SendCommandAsync(new MPD.Connection.Commands.SearchCommand(MpdTags.Artist, mpdArtistId.Value)),
            scope.SendCommandAsync(new MPD.Connection.Commands.SearchCommand(MpdTags.Composer, mpdArtistId.Value)),
            scope.SendCommandAsync(
                new MPD.Connection.Commands.SearchCommand(ExtraMpdTags.Conductor, mpdArtistId.Value)),
            scope.SendCommandAsync(new MPD.Connection.Commands.SearchCommand(ExtraMpdTags.Ensemble, mpdArtistId.Value)),
            scope.SendCommandAsync(new MPD.Connection.Commands.SearchCommand(MpdTags.Performer, mpdArtistId.Value))
        };

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var allTags = results
            .Where(r => r is { IsSuccess: true, Content: not null })
            .SelectMany(r => r.Content!)
            .Select(pair => new Tag(pair.Key, pair.Value))
            .ToList();

        return _albumsParser.GetAlbums(allTags);
    }

    public override async Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token)
    {
        if (resourceId == Id.Empty) return await GetDefaultAlbumResourceStreamAsync(token).ConfigureAwait(false);

        var albumArtId = (AssetId)resourceId;
        var fileName = albumArtId.Value;

        using var scope = await client.CreateConnectionScopeAsync(token: token).ConfigureAwait(false);


        // Try to find the cover from directory the track resides in by looking for a file called cover.png, cover.jpg, or cover.webp.
        try
        {
            long totalBinarySize = 9999;
            long currentSize = 0;
            var data = new List<byte>();

            do
            {
                var (isSuccess, content) = await scope.SendCommandAsync(new AlbumArtCommand(fileName, currentSize))
                    .ConfigureAwait(false);
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
        catch (OperationCanceledException)
        {
            throw;
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get album art from MPD");
        }

        // No album art found. Just return the default album art.
        return await GetDefaultAlbumResourceStreamAsync();
    }

    public override async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (query.Length < 3) return SearchResults.Empty;

        var results = new SearchResults();

        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        results = await AppendFindAsync(MpdTags.Album, scope, results).ConfigureAwait(false);
        results = await AppendFindAsync(MpdTags.Artist, scope, results).ConfigureAwait(false);
        results = await AppendFindAsync(MpdTags.AlbumArtist, scope, results).ConfigureAwait(false);
        results = await AppendFindAsync(MpdTags.Title, scope, results).ConfigureAwait(false);

        return results;

        async Task<SearchResults> AppendFindAsync(ITag tag, ConnectionScope innerScope, SearchResults existingResults)
        {
            var filter = new List<IFilter> { new FilterTag(tag, query, FilterOperator.Contains) };
            var command = new MPD.Connection.Commands.FindCommand(filter);
            var response = await innerScope.SendCommandAsync(command).ConfigureAwait(false);
            return AppendResults(response, ref existingResults);
        }

        SearchResults AppendResults(CommandResult<IEnumerable<KeyValuePair<string, string>>> result, ref SearchResults existingResults)
        {
            if (!result.IsSuccess) return existingResults;
            
            var tags = result.Content!.Select(x => new Tag(x.Key, x.Value)).ToList();
            var albums = _albumsParser.GetAlbums(tags).ToList();

            var foundAlbums = new List<AlbumInfo>();
            var artists = new List<ArtistInfo>();
            var foundTracks = new List<TrackInfo>();
            
            foreach (var album in albums)
            {

                if (album.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    foundAlbums.Add(album);
                }
                
                foreach (var creditArtist in album.CreditsInfo.Artists.Where(a => a.Artist.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                {
                    AddArtist(artists, creditArtist.Artist, creditArtist.Roles);
                }

                foreach (var albumArtist in album.CreditsInfo.AlbumArtists.Where(a =>
                             a.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                {
                    AddArtist(artists, albumArtist, albumArtist.Roles);
                }

                foreach (var track in album.Tracks)
                {
                    if (track.Track.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        foundTracks.Add(track.Track);
                    }
                }
            }

            existingResults = existingResults with
            {
                Albums = existingResults.Albums.Concat(foundAlbums).DistinctBy(a => a.Id).ToList(), 
                Artists = existingResults.Artists.Concat(artists).DistinctBy(a => a.Id).ToList(),
                Tracks = existingResults.Tracks.Concat(foundTracks).DistinctBy(a => a.Id).ToList()
            };

            return existingResults;
        }

        void AddArtist(List<ArtistInfo> artists, ArtistInfo creditArtist, ArtistRoles roles)
        {
            var existingArtist = artists.FirstOrDefault(a => a.Id == creditArtist.Id);
            if (existingArtist != null)
            {
                artists.Remove(existingArtist);
                artists.Add(existingArtist with
                {
                    Roles = existingArtist.Roles | roles
                });
            }
            else
            {
                artists.Add(creditArtist);
            }
        }
    }
}