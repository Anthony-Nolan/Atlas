#nullable enable
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

// Cannot be null, if ints are zero then allele is absent
public record struct HaplotypeKey(int A, int B, int C, int Dqb1, int Drb1);

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

    public HaplotypeKey ConvertWherePossible(string? a, string? b, string? c, string? dqb1, string? drb1)
    {
        var ia = A.Resolve(a);
        var ib = B.Resolve(b);
        var ic = C.Resolve(c);
        var id1 = Dqb1.Resolve(dqb1);
        var id2 = Drb1.Resolve(drb1);
        return new HaplotypeKey(ia, ib, ic, id1, id2);
    }
    
    public LociInfo<string> ReverseLookup(HaplotypeKey key)
        // Named args are essential: LociInfo's positional constructor is (A, B, C, Dpb1, Dqb1, Drb1) - haplotypes have no
        // Dpb1, so passing five positional values would shift Dqb1/Drb1 into the wrong loci and drop Drb1 entirely.
        => new(valueA: A.GetName(key.A), valueB: B.GetName(key.B), valueC: C.GetName(key.C), valueDqb1: Dqb1.GetName(key.Dqb1), valueDrb1: Drb1.GetName(key.Drb1));
}

