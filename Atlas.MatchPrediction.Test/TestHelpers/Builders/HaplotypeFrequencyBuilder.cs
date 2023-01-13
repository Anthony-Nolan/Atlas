using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.Data.Models.HaplotypeFrequency>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    public static class HaplotypeFrequencyBuilder
    {
        public static Builder New => Builder.New;

        public static Builder WithHaplotype(this Builder builder, LociInfo<string> haplotype)
        {
            return builder
                .With(r => r.A, haplotype.A)
                .With(r => r.B, haplotype.B)
                .With(r => r.C, haplotype.C)
                .With(r => r.DQB1, haplotype.Dqb1)
                .With(r => r.DRB1, haplotype.Drb1);
        }

        public static Builder WithDataAt(this Builder builder, Locus locus, string hla)
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

        public static Builder WithFrequency(this Builder builder, decimal frequency)
        {
            return builder.With(f => f.Frequency, frequency);
        }

        public static Builder WithFrequencyAsPercentage(this Builder builder, int frequencyPercentage)
        {
            return builder.With(f => f.Frequency, decimal.Divide(frequencyPercentage, 100));
        }
    }
}