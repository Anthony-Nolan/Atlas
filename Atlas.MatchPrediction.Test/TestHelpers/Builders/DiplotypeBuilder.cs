using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal class DiplotypeBuilder
    {
        private Diplotype diplotype;

        public DiplotypeBuilder()
        {
            var defaultHaplotype = new Haplotype{ Hla = new LociInfo<string>()};
            diplotype = new Diplotype(defaultHaplotype, defaultHaplotype);
        }

        public DiplotypeBuilder WithItem1(Haplotype haplotype)
        {
            diplotype = new Diplotype(haplotype, diplotype.Item2);
            return this;
        }
        
        public DiplotypeBuilder WithItem2(Haplotype haplotype)
        {
            diplotype = new Diplotype(diplotype.Item1, haplotype);
            return this;
        }
        
        public Diplotype Build() => diplotype;
    }
}