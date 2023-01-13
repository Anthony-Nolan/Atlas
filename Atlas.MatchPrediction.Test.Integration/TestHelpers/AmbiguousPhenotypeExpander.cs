using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers
{
    internal class AmbiguousPhenotypeExpander
    {
        public IEnumerable<PhenotypeInfo<string>> LazilyExpandPhenotype(PhenotypeInfo<IReadOnlyCollection<string>> allelesPerLocus)
        {
            return
                from a1 in allelesPerLocus.A.Position1
                from a2 in allelesPerLocus.A.Position2
                from b1 in allelesPerLocus.B.Position1
                from b2 in allelesPerLocus.B.Position2
                from c1 in allelesPerLocus.C.Position1
                from c2 in allelesPerLocus.C.Position2
                from dqb1 in allelesPerLocus.Dqb1.Position1
                from dqb2 in allelesPerLocus.Dqb1.Position2
                from drb1 in allelesPerLocus.Drb1.Position1
                from drb2 in allelesPerLocus.Drb1.Position2
                select new PhenotypeInfo<string>
                (
                    valueA: new LocusInfo<string>(a1, a2),
                    valueB: new LocusInfo<string>(b1, b2),
                    valueC: new LocusInfo<string>(c1, c2),
                    valueDqb1: new LocusInfo<string>(dqb1, dqb2),
                    valueDrb1: new LocusInfo<string>(drb1, drb2)
                );
        }
    }
}