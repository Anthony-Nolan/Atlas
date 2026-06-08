#nullable enable
using System;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

public sealed class AlleleInterner
{
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
    
    public int Resolve(string? allele)
    {
        return TryResolve(allele, out var id) ? id : 0;
    }

    public string? GetName(int id) => toName[id];
}