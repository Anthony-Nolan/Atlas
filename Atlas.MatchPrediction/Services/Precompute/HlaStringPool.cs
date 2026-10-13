using System;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.Precompute;

/// <summary>
/// The de-duplicated HLA names of one <see cref="SubjectGenotypeSet"/>, numbered so that a genotype's 24 slots can be
/// written as small integers instead of 24 strings.
///
/// <para>
/// A pool id is <b>1-based</b>: id <see cref="SubjectGenotypeSetPayloadFormat.NullSentinel"/> means "no HLA at this
/// slot", which is why the format needs no presence bitmap.
/// </para>
///
/// <para>
/// <b>These ids ARE persisted, and they are meaningful only against the pool carried in the same payload.</b> That is
/// the opposite of <c>FrequencySetCacheEntry</c>'s interned haplotype ids, which are process-local, valid only for the
/// lifetime of one cache entry, and must never be written down. Do not carry an id from one payload to another, and do
/// not compare ids across payloads: two payloads with different genotypes will number the same name differently.
/// </para>
/// </summary>
internal sealed class HlaStringPool
{
    private readonly string[] entries;

    /// <summary>
    /// Name to id, and null on a pool rebuilt from a payload.
    ///
    /// <para>
    /// Only the encoder ever looks a name up; the decoder reads ids and turns them back into names through
    /// <see cref="NameOf"/>. Building this on the read path would allocate a dictionary nothing reads, on the one path
    /// the whole format exists to make cheap - ATL-221 runs it per donor per search.
    /// </para>
    /// </summary>
    private readonly Dictionary<string, int> idsByName;

    private HlaStringPool(string[] entries, Dictionary<string, int> idsByName)
    {
        this.entries = entries;
        this.idsByName = idsByName;
    }

    /// <summary>Number of real entries. Ids run from 1 to this value inclusive.</summary>
    internal int Count => entries.Length;

    /// <summary>The entries in id order, i.e. <c>Entries[0]</c> is id 1. What the encoder writes.</summary>
    internal IReadOnlyList<string> Entries => entries;

    /// <summary>
    /// The union of every non-null name at every slot of <b>both</b> resolutions, sorted with
    /// <see cref="StringComparer.Ordinal"/>.
    ///
    /// <para>
    /// Ordinal is a correctness requirement, not a preference: <c>OrderBy(s =&gt; s)</c> is culture-sensitive, so the
    /// same set would encode to different bytes on a machine with a different current culture, and
    /// <c>Encode_IsDeterministic</c> would pass on the build agent and fail in production. Sorting also puts names
    /// that share a long prefix next to each other, which is what DEFLATE's window feeds on.
    /// </para>
    /// </summary>
    internal static HlaStringPool Build(IReadOnlyList<GenotypeAtDesiredResolutions> genotypes)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var slots = new string[SubjectGenotypeSetPayloadFormat.SlotCount];

        foreach (var genotype in genotypes)
        {
            Collect(genotype.StringMatchableResolution, slots, names);
            Collect(genotype.HaplotypeResolution, slots, names);
        }

        var entries = new string[names.Count];
        names.CopyTo(entries);
        Array.Sort(entries, StringComparer.Ordinal);

        return new HlaStringPool(entries, BuildIndex(entries));
    }

    /// <summary>
    /// Rebuilds a pool from the entries a payload carries, which are already in id order. Read-only: it carries no
    /// name-to-id index, so <see cref="IdOf"/> throws on it.
    /// </summary>
    internal static HlaStringPool FromOrderedEntries(string[] orderedEntries) => new(orderedEntries, idsByName: null);

    /// <summary>The id of <paramref name="name"/>, or the null sentinel when it is null.</summary>
    /// <exception cref="InvalidOperationException">This pool came from <see cref="FromOrderedEntries"/>.</exception>
    internal int IdOf(string name)
    {
        if (name == null)
        {
            return SubjectGenotypeSetPayloadFormat.NullSentinel;
        }

        if (idsByName == null)
        {
            throw new InvalidOperationException(
                "This pool was rebuilt from a payload and holds no name-to-id index; only the encoder assigns ids.");
        }

        // Unreachable while the pool is built from the same set being encoded. Worth an exception rather than a
        // silent null, because the alternative failure is a payload that decodes to the wrong HLA.
        return idsByName.TryGetValue(name, out var id)
            ? id
            : throw new InvalidOperationException($"HLA name '{name}' is not in the pool it is being encoded against.");
    }

    /// <summary>
    /// The name with this id, or null for the sentinel. Every slot that shares a name gets the SAME string reference -
    /// the reason the pool is worth having on the read path, where it turns ~24,600 string allocations per donor into
    /// the pool's own ~200.
    /// </summary>
    internal string NameOf(int id) => id == SubjectGenotypeSetPayloadFormat.NullSentinel ? null : entries[id - 1];

    internal bool IsValidId(int id) => id >= SubjectGenotypeSetPayloadFormat.NullSentinel && id <= entries.Length;

    private static void Collect(PhenotypeInfo<string> resolution, string[] slots, HashSet<string> names)
    {
        SubjectGenotypeSetPayloadFormat.ToSlots(resolution, slots);

        foreach (var name in slots)
        {
            if (name != null)
            {
                names.Add(name);
            }
        }
    }

    private static Dictionary<string, int> BuildIndex(string[] orderedEntries)
    {
        var index = new Dictionary<string, int>(orderedEntries.Length, StringComparer.Ordinal);
        for (var i = 0; i < orderedEntries.Length; i++)
        {
            index[orderedEntries[i]] = i + 1;
        }

        return index;
    }
}
