#nullable enable
using System.Collections.Generic;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// Dense sequential ids for distinct keys, first-seen-wins. The dictionary+list idiom shared by
/// <see cref="AlleleInterner"/> (interning a single locus's allele string) and
/// <c>CompressedPhenotypeExpander.InternHaplotypeNames</c> (interning a whole haplotype's name form) - kept in one
/// place so a future fix to interning semantics applies to both.
/// </summary>
internal sealed class Interner<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, int> idByKey = new();
    private readonly List<TKey> keyById = new();

    /// <summary>Assigns a new id if <paramref name="key"/> is unseen, otherwise returns its existing one.</summary>
    public int Intern(TKey key)
    {
        if (!idByKey.TryGetValue(key, out var id))
        {
            id = keyById.Count;
            idByKey[key] = id;
            keyById.Add(key);
        }

        return id;
    }

    /// <summary>Query time: never mints new ids. Miss => key not seen by this interner.</summary>
    public bool TryGetId(TKey key, out int id) => idByKey.TryGetValue(key, out id);

    /// <summary>Dense ids from 0, one entry per distinct key interned, in first-seen order.</summary>
    public IReadOnlyList<TKey> KeyById => keyById;
}
