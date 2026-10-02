using System;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities;

/// <summary>
/// Turns an <see cref="AllowedLociKey"/> into the locus set it names.
///
/// <para>
/// Here, next to the enum, rather than in the service that uses it: the enum and the sets it stands for are one
/// fact, and splitting them is how the two drift apart. Dpb1 is in none of them - match prediction never imputes it
/// (<c>LocusSettings.MatchPredictionLoci</c>).
/// </para>
/// </summary>
public static class AllowedLociKeyExtensions
{
    private static readonly Dictionary<AllowedLociKey, IReadOnlySet<Locus>> LociByKey = new()
    {
        { AllowedLociKey.ABCDrb1Dqb1, new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Drb1, Locus.Dqb1 } },
        { AllowedLociKey.ABCDrb1, new HashSet<Locus> { Locus.A, Locus.B, Locus.C, Locus.Drb1 } },
        { AllowedLociKey.ABDrb1Dqb1, new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1, Locus.Dqb1 } },
        { AllowedLociKey.ABDrb1, new HashSet<Locus> { Locus.A, Locus.B, Locus.Drb1 } }
    };

    /// <summary>The four keys, in the order the enum declares them.</summary>
    public static IReadOnlyList<AllowedLociKey> All { get; } =
    [
        AllowedLociKey.ABCDrb1Dqb1,
        AllowedLociKey.ABCDrb1,
        AllowedLociKey.ABDrb1Dqb1,
        AllowedLociKey.ABDrb1
    ];

    public static IReadOnlySet<Locus> ToLoci(this AllowedLociKey key) =>
        LociByKey.TryGetValue(key, out var loci)
            ? loci
            : throw new ArgumentOutOfRangeException(nameof(key), key, "Not one of the four allowed-loci combinations.");

    /// <summary>The key naming exactly this locus set, for turning a caller's set back into a stored value.</summary>
    public static AllowedLociKey ToAllowedLociKey(IReadOnlySet<Locus> loci) =>
        TryToAllowedLociKey(loci, out var key)
            ? key
            : throw new ArgumentOutOfRangeException(nameof(loci), string.Join(",", loci), "Not one of the four allowed-loci combinations.");

    /// <summary>
    /// As <see cref="ToAllowedLociKey"/>, but returns false instead of throwing for a set that is not one of the four.
    /// For search, where an uncovered set is a reason to compute live, not an error.
    /// </summary>
    public static bool TryToAllowedLociKey(IReadOnlySet<Locus> loci, out AllowedLociKey key)
    {
        foreach (var (candidate, keyLoci) in LociByKey)
        {
            if (keyLoci.SetEquals(loci))
            {
                key = candidate;
                return true;
            }
        }

        key = default;
        return false;
    }
}
