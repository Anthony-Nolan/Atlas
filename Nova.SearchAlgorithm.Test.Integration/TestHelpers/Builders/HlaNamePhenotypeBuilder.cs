using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
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

        public HlaNamePhenotypeBuilder WithHlaNameAt(Locus locus, TypePositions positions, string hlaName)
        {
            hlaNamePhenotype.SetAtPosition(locus, positions, hlaName);
            return this;
        }

        public PhenotypeInfo<string> Build()
        {
            return hlaNamePhenotype;
        }
    }
}
