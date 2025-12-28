using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure.Tagging;
using MpcNET.Commands.Database;
using MpcNET.Tags;
using FindCommand = Aria.MusicServers.MPD.Commands.FindCommand;

namespace Aria.MusicServers.MPD;

// TODO: This file is a work in progress and requires significant performance optimizations.
public class Library(Session session, ITagParser tagParser) : ILibrary
{
    public async Task<IEnumerable<ArtistInfo>> GetArtists()
    {
        var artistMap = new Dictionary<string, ArtistInfo>(StringComparer.OrdinalIgnoreCase);

        var artistsResponse = await session.SendCommandAsync(new ListCommand(MpdTags.Artist));
        foreach (var name in artistsResponse) AddOrUpdate(name, ArtistRoles.Performer);

        var composersResponse = await session.SendCommandAsync(new ListCommand(MpdTags.Composer));
        foreach (var name in composersResponse) AddOrUpdate(name, ArtistRoles.Composer);

        var performerResponse = await session.SendCommandAsync(new ListCommand(MpdTags.Performer));
        foreach (var name in performerResponse) AddOrUpdate(name, ArtistRoles.Performer);

        var conductorResponse = await session.SendCommandAsync(new ListCommand(ExtraMpdTags.Conductor));
        foreach (var name in conductorResponse) AddOrUpdate(name, ArtistRoles.Conductor);

        var ensembleResponse = await session.SendCommandAsync(new ListCommand(ExtraMpdTags.Ensemble));
        foreach (var name in ensembleResponse) AddOrUpdate(name, ArtistRoles.Ensemble);

        return artistMap.Values;

        void AddOrUpdate(string artistName, ArtistRoles roles)
        {
            if (string.IsNullOrWhiteSpace(artistName)) return;

            if (artistMap.TryGetValue(artistName, out var existingArtist))
            {
                artistMap[artistName] = existingArtist with
                {
                    Roles = existingArtist.Roles | roles
                };
            }
            else
            {
                var id = new ArtistId(artistName);
                artistMap[artistName] = new ArtistInfo(id, artistName, string.Empty, roles);
            }
        }
    }

    public async Task<IEnumerable<AlbumInfo>> GetAlbums()
    {
        var albumMap = new Dictionary<AlbumId, AlbumInfo>();

        var albumsResponse = await session.SendCommandAsync(new ListCommand(MpdTags.Album));
        foreach (var name in albumsResponse) AddOrUpdate(name);

        return albumMap.Values;

        void AddOrUpdate(string albumName)
        {
            if (string.IsNullOrWhiteSpace(albumName)) return;

            var albumId = new AlbumId(albumName);

            if (albumMap.TryGetValue(albumId, out var existingAlbum))
                albumMap[albumId] = existingAlbum with
                {
                    CreditsInfo = existingAlbum.CreditsInfo with
                    {
                        // TODO: new info here
                    }
                };
            else
                albumMap[albumId] = new AlbumInfo
                {
                    Id = new AlbumId(albumName),
                    Title = albumName,
                    CreditsInfo = new AlbumCreditsInfo
                    {
                        // Todo: add credits info here
                    }
                };
        }
    }

    public async Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId)
    {
        var mpdArtistId = (ArtistId)artistId;

        // TODO: Maybe I can combine this in one call for performance
        var pairs = new List<IEnumerable<KeyValuePair<string, string>>>
        {
            await session.SendCommandAsync(new FindCommand(MpdTags.AlbumArtist, mpdArtistId.Value)),
            await session.SendCommandAsync(new FindCommand(MpdTags.Composer, mpdArtistId.Value)),
            await session.SendCommandAsync(new FindCommand(ExtraMpdTags.Conductor, mpdArtistId.Value)),
            await session.SendCommandAsync(new FindCommand(ExtraMpdTags.Ensemble, mpdArtistId.Value)),
            await session.SendCommandAsync(new FindCommand(MpdTags.Performer, mpdArtistId.Value))
        }.SelectMany(x => x);

        // These responses are separated by 'File' keys in the key–value pairs.
        // For each group, create a separate List<KeyValuePair> to allow parsing per song.
        var currentSongPairs = new List<KeyValuePair<string, string>>();
        var albumInfoList = new List<AlbumInfo>();
        foreach (var pair in pairs)
        {
            // Each 'file' key marks the start of a new song in the response stream
            if (pair.Key.Equals("file", StringComparison.OrdinalIgnoreCase) && currentSongPairs.Count > 0)
            {
                // Process the song for album information 
                albumInfoList.Add(ParseAlbumInformation(currentSongPairs));
                currentSongPairs.Clear();
            }

            currentSongPairs.Add(pair);
        }
        
        // // To not miss the last song
        if (currentSongPairs.Count > 0) albumInfoList.Add(ParseAlbumInformation(currentSongPairs)); 

        // Squash all the information into unique albums
        var uniqueAlbumInfos = albumInfoList.GroupBy(a => a.Id)
            .Select(group =>
            {
                var first = group.First();

                return group.Aggregate(first,
                    (current, albumInfo) => current with
                    {
                        CreditsInfo = current.CreditsInfo with
                        {
                            AlbumArtists = current.CreditsInfo.AlbumArtists
                                .Union(albumInfo.CreditsInfo.AlbumArtists.ToList()).ToList(),
                            Artists = current.CreditsInfo.Artists.Union(albumInfo.CreditsInfo.Artists.ToList())
                                .ToList()
                        }
                    });
            });

        return uniqueAlbumInfos;

        AlbumInfo ParseAlbumInformation(List<KeyValuePair<string, string>> songPairs)
        {
            // Determine the album ID.
            // TODO As AlbumId states, we need to add fields to identify unique albums.
            var album = songPairs
                .Single(p => p.Key.Equals(MpdTags.Album.Value.ToLower(), StringComparison.OrdinalIgnoreCase)).Value;
            var albumId = new AlbumId(album);
            
            return tagParser.ParseAlbumInformation(albumId, songPairs);
        }
    }
}