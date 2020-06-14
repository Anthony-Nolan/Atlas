using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    public interface IAmbiguousPhenotypeExpander
    {
        /// <summary>
        ///  Returns all possible unambiguous genotypes by calculating the cartesian product of a set of possible alleles at each position.
        /// </summary>
        public IEnumerable<PhenotypeInfo<string>> ExpandPhenotype(PhenotypeInfo<IReadOnlyCollection<string>> allelesPerLocus);
    }

    public class AmbiguousPhenotypeExpander : IAmbiguousPhenotypeExpander
    {
        public IEnumerable<PhenotypeInfo<string>> ExpandPhenotype(PhenotypeInfo<IReadOnlyCollection<string>> allelesPerLocus)
        {
            var genotypes = (from a1 in allelesPerLocus.A.Position1
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
                {
                    A = new LocusInfo<string> { Position1 = a1, Position2 = a2 },
                    B = new LocusInfo<string> { Position1 = b1, Position2 = b2 },
                    C = new LocusInfo<string> { Position1 = c1, Position2 = c2 },
                    Dqb1 = new LocusInfo<string> { Position1 = dqb1, Position2 = dqb2 },
                    Drb1 = new LocusInfo<string> { Position1 = drb1, Position2 = drb2 }
                });

            return genotypes;
        }
    }
}
