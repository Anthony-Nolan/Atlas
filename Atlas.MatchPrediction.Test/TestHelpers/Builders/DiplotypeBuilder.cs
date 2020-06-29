using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal static class DiplotypeBuilder
    {
        internal static Builder<Diplotype> New => Builder<Diplotype>.New
            .With(d => d.Item1, new Haplotype {Hla = new LociInfo<string>()})
            .With(d => d.Item2, new Haplotype {Hla = new LociInfo<string>()});
    }
}