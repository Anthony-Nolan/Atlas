using Atlas.MatchPrediction.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    public static class DiplotypeBuilder
    {
        public static Builder<Diplotype> New => Builder<Diplotype>.New
            .With(d => d.Item1, new Haplotype())
            .With(d => d.Item1, new Haplotype());
    }
}