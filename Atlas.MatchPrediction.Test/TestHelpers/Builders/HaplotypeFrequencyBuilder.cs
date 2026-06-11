using System;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchPrediction.Data.Models.HaplotypeFrequency>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders;

public static class HaplotypeFrequencyBuilder
{
    public static Composer New => FixtureBuilder.For<Atlas.MatchPrediction.Data.Models.HaplotypeFrequency>();

    public static Composer WithHaplotype(this Composer builder, LociInfo<string> haplotype)
    {
        return builder
            .With(r => r.A, haplotype.A)
            .With(r => r.B, haplotype.B)
            .With(r => r.C, haplotype.C)
            .With(r => r.DQB1, haplotype.Dqb1)
            .With(r => r.DRB1, haplotype.Drb1);
    }

    public static Composer WithDataAt(this Composer builder, Locus locus, string hla)
    {
        return locus switch
        {
            Locus.A => builder.With(r => r.A, hla),
            Locus.B => builder.With(r => r.B, hla),
            Locus.C => builder.With(r => r.C, hla),
            Locus.Dpb1 => throw new ArgumentException(),
            Locus.Dqb1 => builder.With(r => r.DQB1, hla),
            Locus.Drb1 => builder.With(r => r.DRB1, hla),
            _ => throw new ArgumentOutOfRangeException(nameof(locus), locus, null)
        };
    }

    public static Composer WithFrequency(this Composer builder, decimal frequency)
    {
        return builder.With(f => f.Frequency, frequency);
    }

    public static Composer WithFrequencyAsPercentage(this Composer builder, int frequencyPercentage)
    {
        return builder.With(f => f.Frequency, decimal.Divide(frequencyPercentage, 100));
    }
}