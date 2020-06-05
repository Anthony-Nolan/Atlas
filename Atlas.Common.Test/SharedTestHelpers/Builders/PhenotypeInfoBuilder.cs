using Atlas.Common.GeneticData.PhenotypeInfo;
using LochNessBuilder;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    public static class PhenotypeInfoBuilder
    {
        public static Builder<PhenotypeInfo<string>> New => Builder<PhenotypeInfo<string>>.New
            .With(d => d.A , new LocusInfo<string> { Position1 = "A1:A1", Position2 = "A2:A2" })
            .With(d => d.B, new LocusInfo<string> { Position1 = "B1:B1", Position2 = "B2:B2" })
            .With(d => d.C, new LocusInfo<string> { Position1 = "C1:C1", Position2 = "C2:C2" })
            .With(d => d.Dpb1, new LocusInfo<string> { Position1 = "Dpb11:Dpb11", Position2 = "Dpb12:Dpb12" })
            .With(d => d.Dqb1, new LocusInfo<string> { Position1 = "Dqb11:Dqb11", Position2 = "Dqb12:Dqb12" })
            .With(d => d.Drb1, new LocusInfo<string> { Position1 = "Drb11:Drb11", Position2 = "Drb12:Drb12" });
    }
}
