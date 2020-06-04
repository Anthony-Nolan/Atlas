using Atlas.Common.GeneticData.PhenotypeInfo;
using LochNessBuilder;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    public static class PhenotypeInfoBuilder
    {
        public static Builder<PhenotypeInfo<string>> New => Builder<PhenotypeInfo<string>>.New
            .With(d => d.A, new LocusInfo<string> { Position1 = "A-1", Position2 = "A-2" })
            .With(d => d.B, new LocusInfo<string> { Position1 = "B-1", Position2 = "B-2" })
            .With(d => d.C, new LocusInfo<string> { Position1 = "C-1", Position2 = "C-2" })
            .With(d => d.Dpb1, new LocusInfo<string> { Position1 = "Dpb1-1", Position2 = "Dpb1-2" })
            .With(d => d.Dqb1, new LocusInfo<string> { Position1 = "Dqb1-1", Position2 = "Dqb1-2" })
            .With(d => d.Drb1, new LocusInfo<string> { Position1 = "Drb1-1", Position2 = "Drb1-2" });
    }
}
