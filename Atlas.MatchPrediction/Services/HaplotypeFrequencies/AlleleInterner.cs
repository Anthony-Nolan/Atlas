#nullable enable

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

public sealed class AlleleInterner
{
    // Returned by Resolve when an allele is not present in this set.
    // Deliberately distinct from 0 ("absent/untyped") so callers can tell the two cases apart.
    // This sentinel is for lookups only: it must never be stored as a key, persisted, or passed to GetName.
    public const int NotFound = -1;

    // Ids here are offset by 1 from the shared interner's: id 0 is reserved for "absent/untyped", so a real allele's
    // id is 1 + its Interner<string> id.
    private readonly Interner<string> inner = new();

    // Load time: assigns a new id if unseen.
    public int Intern(string? allele)
    {
        if (string.IsNullOrEmpty(allele)) return 0;
        return 1 + inner.Intern(allele);
    }

    // Query time: never mints new ids. Miss => allele absent from this set.
    public bool TryResolve(string? allele, out int id)
    {
        if (!string.IsNullOrEmpty(allele))
        {
            if (inner.TryGetId(allele, out var innerId))
            {
                id = 1 + innerId;
                return true;
            }

            id = 0;
            return false;
        }

        id = 0;
        return true;
    }

    // Query time: returns 0 for an untyped (null/empty) allele, the interned id for a known allele,
    // or NotFound when the allele is absent from this set. Use this (not 0) to distinguish "not in set"
    // from "untyped" - the result is safe for dictionary lookups but must not be passed to GetName.
    public int Resolve(string? allele)
    {
        return TryResolve(allele, out var id) ? id : NotFound;
    }

    // Maps any non-positive id (including the NotFound sentinel) back to "no allele", keeping ReverseLookup total.
    public string? GetName(int id) => id <= 0 ? null : inner.KeyById[id - 1];

    // One past the highest id this interner has minted - ids are dense from 0, so this sizes an array indexed by id.
    // That array is what lets the pool filter test an allele without hashing anything.
    internal int IdCount => inner.KeyById.Count + 1;
}
