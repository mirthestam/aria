using System.Collections.Concurrent;
using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using MpcNET;
using MpcNET.Commands.Database;
using MpcNET.Tags;

namespace Aria.Backends.MPD;

public partial class Library
{
    // Artists can appear with multiple notations in our backend.
    // This means we 'deduplicated' them using our tag parser.
    // However, for a lookup, we need to use those aliases to make sure
    // we are leaving nothing out.
    private readonly ConcurrentDictionary<Id, ConcurrentDictionary<string, byte>> _artistAliases = new();
    
    // Artists
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

        await FetchAndAddSingles(new ListCommand(MpdTags.AlbumArtist), ArtistRoles.Main, scope).ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(MpdTags.Artist), ArtistRoles.Performer, scope).ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(MpdTags.Composer), ArtistRoles.Composer, scope).ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(MpdTags.Performer), ArtistRoles.Performer, scope).ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(ExtraMpdTags.Conductor), ArtistRoles.Conductor, scope).ConfigureAwait(false);
        await FetchAndAddSingles(new ListCommand(ExtraMpdTags.Ensemble), ArtistRoles.Ensemble, scope).ConfigureAwait(false);

        // Disabled because sorting matching is still buggy and needs work
        //await FetchAndAddDoubles(new ListCommand(ExtraMpdTags.ComposerSort, MpdTags.Composer), ArtistRoles.Composer, scope).ConfigureAwait(false);

        return artistMap.Values;

        async Task FetchAndAddSingles(IMpcCommand<IEnumerable<string>> command, ArtistRoles role,
            ConnectionScope connectionScope)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (isSuccess, result) = await connectionScope.SendCommandAsync(command).ConfigureAwait(false);
            if (isSuccess && result != null)
            {
                foreach (var name in result)
                    AddOrUpdate(backendArtistName: name, artistNameSort: null, roles: role);
            }
        }
        
        void AddOrUpdate(string backendArtistName, string? artistNameSort, ArtistRoles roles)
        {
            if (string.IsNullOrWhiteSpace(backendArtistName)) return;
            
            // Parse the info we retrieve.
            var info = tagParser.ParseArtistInformation(backendArtistName, artistNameSort, roles);
            if (info == null) return;
            if (string.IsNullOrWhiteSpace(info.Name)) return;
            
            var id = new ArtistId(info.Name);
            
            if (artistMap.TryGetValue(id, out var existingArtist))
            {
                artistMap[id] = existingArtist with
                {
                    NameSort = info.NameSort ?? existingArtist.NameSort,
                    Roles = existingArtist.Roles | info.Roles
                };
            }
            else
            {
                artistMap[id] = info with { Id = id };
            }
            
            // Record the backend/original value as an alias for later queries
            _artistAliases.GetOrAdd(id, _ => new ConcurrentDictionary<string, byte>())[backendArtistName] = 0;
        }
    }
}