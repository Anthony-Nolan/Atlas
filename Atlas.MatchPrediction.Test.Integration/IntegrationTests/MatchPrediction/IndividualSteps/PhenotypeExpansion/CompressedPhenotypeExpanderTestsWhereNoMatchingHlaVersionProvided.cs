using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using FluentAssertions;
using NUnit.Framework;
using static Atlas.MatchPrediction.Test.Integration.Resources.Alleles.Alleles;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.PhenotypeExpansion
{
    [TestFixture]
    internal class CompressedPhenotypeExpanderTestsWhereNoMatchingHlaVersionProvided : CompressedPhenotypeExpanderTestsBase
    {

        [Test]
        public async Task ExpandCompressedPhenotype_ContainsAlleleFromLaterHlaVersion_ReturnsEmptySet()
        {
            // this allele was introduced in the HLA version v3340 (HF set is on v3330),
            // and maps to the same G group found in the test haplotype 1 (A*02:01:01G)
            const string alleleFromLaterHlaVersion = "02:01:01:42";

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, LocusPosition.One, alleleFromLaterHlaVersion)
                .Build();

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion
            };

            var haplotypes = new List<LociInfo<string>> { HaplotypeBuilder1.Build(), HaplotypeBuilder2.Build() };
            var allHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
            {
                GGroup = haplotypes,
                PGroup = haplotypes,
                SmallGGroup = haplotypes
            };
            
            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input, allHaplotypes);

            // Expect the HLA lookup to fail, but HMD exception should be suppressed and instead no genotypes should be returned
            genotypes.Should().BeEmpty();
        }
    }
}
