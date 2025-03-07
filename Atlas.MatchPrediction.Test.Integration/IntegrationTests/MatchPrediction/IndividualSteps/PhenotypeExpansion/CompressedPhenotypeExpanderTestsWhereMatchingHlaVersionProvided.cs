﻿using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
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
            var setId = await ImportHaplotypeFrequencies(new[] { HaplotypeBuilder1.Build(), HaplotypeBuilder2.Build() });

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
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input);

            // Not asserting the exact genotypes generated
            // Rather, it is enough to assert that genotypes are returned, as if the HLA lookup fails, no genotypes would be returned
            genotypes.Should().NotBeEmpty();
        }

        [Test]
        public async Task ExpandCompressedPhenotype_ContainsAlleleFromLaterHlaVersion_AlleleIsNotFoundInMatchingHlaVersion_ThrowsException()
        {
            var setId = await ImportHaplotypeFrequencies(new[] { HaplotypeBuilder1.Build(), HaplotypeBuilder2.Build() });

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
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            var exception = Assert.ThrowsAsync<HlaMetadataDictionaryException>(async () => await Expander.ExpandCompressedPhenotype(input));
            exception.Message.Should().Contain(alleleFromLaterHlaVersion);
        }
    }
}
