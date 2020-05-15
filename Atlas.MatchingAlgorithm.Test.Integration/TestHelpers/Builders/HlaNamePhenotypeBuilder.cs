using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class HlaNamePhenotypeBuilder
    {
        private readonly PhenotypeInfo<string> hlaNamePhenotype;

        public HlaNamePhenotypeBuilder()
        {
            hlaNamePhenotype = new PhenotypeInfo<string>();
        }

        public HlaNamePhenotypeBuilder(PhenotypeInfo<string> sourcePhenotype)
        {
            hlaNamePhenotype = sourcePhenotype.Map((locus, positions, hlaName) => hlaName);
        }

        public HlaNamePhenotypeBuilder WithHlaNameAt(Locus locus, TypePosition position, string hlaName)
        {
            hlaNamePhenotype.SetAtPosition(locus, position, hlaName);
            return this;
        }

        public PhenotypeInfo<string> Build()
        {
            return hlaNamePhenotype;
        }
    }
}
