using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using FluentAssertions;
using NUnit.Framework;
using static Atlas.MatchPrediction.Test.Integration.Resources.Alleles.Alleles;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.PhenotypeExpansion
{
    [TestFixture]
    internal class CompressedPhenotypeExpanderTestsWhereMatchingHlaVersionProvided : CompressedPhenotypeExpanderTestsBase
    {
        private const string MatchingHlaVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;

        public CompressedPhenotypeExpanderTestsWhereMatchingHlaVersionProvided() : base(MatchingHlaVersion)
        {
        }

        [Test]
        public async Task ExpandCompressedPhenotype_ContainsAlleleFromLaterHlaVersion_AlleleIsValidInMatchingHlaVersion_ExpandsPhenotype()
        {
            // this allele was introduced in the matching HLA version v3400 (HF set is on v3330, and matching on v3400),
            // and maps to the same G group found in the test haplotype 1 (A*02:01:01G)
            const string alleleFromLaterHlaVersion = "02:01:01:135";

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, LocusPosition.One, alleleFromLaterHlaVersion)
                .Build();

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, MatchingHlaVersion),
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

            // Not asserting the exact genotypes generated
            // Rather, it is enough to assert that genotypes are returned, as if the HLA lookup fails, no genotypes would be returned
            genotypes.Should().NotBeEmpty();
        }

        [Test]
        public async Task ExpandCompressedPhenotype_ContainsAlleleFromLaterHlaVersion_AlleleIsNotFoundInMatchingHlaVersion_ReturnsEmptySet()
        {
            // this allele was introduced in HLA version v3440 (HF set is on v3330, and matching on v3400),
            // and maps to the same G group found in the test haplotype 1 (A*02:01:01G)
            const string alleleFromLaterHlaVersion = "02:01:01:170";

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, LocusPosition.One, alleleFromLaterHlaVersion)
                .Build();

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, MatchingHlaVersion),
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

            // Expect the HLA lookup to fail, but HMD exceptions should be suppressed and instead no genotypes should be returned
            genotypes.Should().BeEmpty();
        }
    }
}
