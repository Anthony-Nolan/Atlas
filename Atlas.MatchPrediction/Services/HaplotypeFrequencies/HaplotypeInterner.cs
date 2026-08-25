#nullable enable
using System;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

// Cannot be null, if ints are zero then allele is absent
public record struct HaplotypeKey(int A, int B, int C, int Dqb1, int Drb1)
{
    /// <summary>
    /// The interned allele id at <paramref name="locus"/>. ATL-233 T1 follow-up: the pool filter compares ids rather
    /// than allele names, so it reads a key positionally, once per allowed locus per pooled haplotype.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A haplotype is not typed at Dpb1, so it holds no id there. <c>LocusSettings.MatchPredictionLoci</c> excludes it,
    /// which is why this can throw rather than return "absent" - reaching it means the allowed loci are wrong.
    /// </exception>
    internal readonly int GetLocus(Locus locus) => locus switch
    {
        Locus.A => A,
        Locus.B => B,
        Locus.C => C,
        Locus.Dqb1 => Dqb1,
        Locus.Drb1 => Drb1,
        _ => throw new ArgumentOutOfRangeException(nameof(locus), locus, "Haplotypes carry no allele at this locus.")
    };
}

public record struct HaplotypeFrequencyValue(decimal Frequency, HaplotypeTypingCategory TypingCategory);

public sealed class HaplotypeInterner
{
    public AlleleInterner A { get; } = new();

    public AlleleInterner B { get; } = new();

    public AlleleInterner C { get; } = new();

    public AlleleInterner Dqb1 { get; } = new();

    public AlleleInterner Drb1 { get; } = new();

    // Creates a new haplotype key from the given allele strings
    // Use this when building a haplotype set cache
    public HaplotypeKey Intern(string? a, string? b, string? c, string? dqb1, string? drb1)
        => new(A.Intern(a), B.Intern(b), C.Intern(c), Dqb1.Intern(dqb1), Drb1.Intern(drb1));

    public bool TryResolve(string? a, string? b, string? c, string? dqb1, string? drb1,
        out HaplotypeKey key)
    {
        key = default;
        if (
            !A.TryResolve(a, out var ia)
         || !B.TryResolve(b, out var ib)
         || !C.TryResolve(c, out var ic)
         || !Dqb1.TryResolve(dqb1, out var id1)
         || !Drb1.TryResolve(drb1, out var id2)
        )
        {
            return false; // some allele isn't in this set => frequency is 0
        }
        key = new HaplotypeKey(ia, ib, ic, id1, id2);
        return true;
    }

    // Resolves each allele independently: 0 for untyped loci, the interned id for known alleles,
    // and AlleleInterner.NotFound for alleles absent from this set. A NotFound at a non-excluded locus
    // guarantees a lookup miss (frequency 0), which is correct - the set has never seen that allele.
    // The returned key is for lookups only and must not be passed to ReverseLookup.
    public HaplotypeKey ConvertWherePossible(string? a, string? b, string? c, string? dqb1, string? drb1)
    {
        var ia = A.Resolve(a);
        var ib = B.Resolve(b);
        var ic = C.Resolve(c);
        var id1 = Dqb1.Resolve(dqb1);
        var id2 = Drb1.Resolve(drb1);
        return new HaplotypeKey(ia, ib, ic, id1, id2);
    }
    
    /// <summary>
    /// The per-locus interner backing <see cref="HaplotypeKey.GetLocus"/>'s ids, so a caller can resolve the subject's
    /// own allele names into the same id space before it starts comparing.
    /// </summary>
    internal AlleleInterner ForLocus(Locus locus) => locus switch
    {
        Locus.A => A,
        Locus.B => B,
        Locus.C => C,
        Locus.Dqb1 => Dqb1,
        Locus.Drb1 => Drb1,
        _ => throw new ArgumentOutOfRangeException(nameof(locus), locus, "Haplotypes carry no allele at this locus.")
    };

    public LociInfo<string> ReverseLookup(HaplotypeKey key)
        // Named args are essential: LociInfo's positional constructor is (A, B, C, Dpb1, Dqb1, Drb1) - haplotypes have no
        // Dpb1, so passing five positional values would shift Dqb1/Drb1 into the wrong loci and drop Drb1 entirely.
        => new(valueA: A.GetName(key.A), valueB: B.GetName(key.B), valueC: C.GetName(key.C), valueDqb1: Dqb1.GetName(key.Dqb1), valueDrb1: Drb1.GetName(key.Drb1));
}

