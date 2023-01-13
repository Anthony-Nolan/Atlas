using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    internal interface IAmbiguousPhenotypeExpander
    {
        /// <summary>
        ///  Returns all possible unambiguous genotypes by calculating the cartesian product of a set of possible alleles at each position.
        /// </summary>
        public ISet<PhenotypeInfo<string>> ExpandPhenotype(PhenotypeInfo<IReadOnlyCollection<string>> allelesPerLocus);

        /// <returns>
        /// An un-reified IEnumerable, representing all possible unambiguous genotypes by calculating the cartesian product of a set of possible alleles at each position.
        /// </returns>
        public IEnumerable<PhenotypeInfo<string>> LazilyExpandPhenotype(PhenotypeInfo<IReadOnlyCollection<string>> allelesPerLocus);
    }

    internal class AmbiguousPhenotypeExpander : IAmbiguousPhenotypeExpander
    {
        public ISet<PhenotypeInfo<string>> ExpandPhenotype(PhenotypeInfo<IReadOnlyCollection<string>> allelesPerLocus)
        {
            var genotypes = LazilyExpandPhenotype(allelesPerLocus);
            return genotypes.ToHashSet();
        }

        /// <inheritdoc />
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