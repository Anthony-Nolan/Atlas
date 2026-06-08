#nullable enable
using System;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

public sealed class AlleleInterner
{
    // Returned by Resolve when an allele is not present in this set.
    // Deliberately distinct from 0 ("absent/untyped") so callers can tell the two cases apart.
    // This sentinel is for lookups only: it must never be stored as a key, persisted, or passed to GetName.
    public const int NotFound = -1;

    private readonly Dictionary<string, int> toId = new(StringComparer.Ordinal);
    private readonly List<string?> toName = [null]; // index 0 = "absent"

    // Load time: assigns a new id if unseen.
    public int Intern(string? allele)
    {
        if (string.IsNullOrEmpty(allele)) return 0;
        if (toId.TryGetValue(allele, out var id)) return id;
        id = toName.Count;
        toId.Add(allele, id);
        toName.Add(allele);
        return id;
    }

    // Query time: never mints new ids. Miss => allele absent from this set.
    public bool TryResolve(string? allele, out int id)
    {
        if (!string.IsNullOrEmpty(allele))
        {
            return toId.TryGetValue(allele, out id);
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
    public string? GetName(int id) => id <= 0 ? null : toName[id];
}