using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using FluentAssertions;
using NUnit.Framework;
using static Atlas.MatchPrediction.Test.Integration.Resources.Alleles.Alleles;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.PhenotypeExpansion
{
    [TestFixture]
    internal class FilteredExpansionWhereHlaExceptionsAreAllowedTests : FilteredExpansionTestsBase
    {
        private const bool SuppressCompressedPhenotypeConversionExceptions = false;

        /// <inheritdoc />
        public FilteredExpansionWhereHlaExceptionsAreAllowedTests() : base(SuppressCompressedPhenotypeConversionExceptions)
        {
        }

        [Test]
        public void ExpandCompressedPhenotype_ContainsAlleleFromLaterHlaVersion_ThrowsHlaMetadataDictionaryException()
        {
            // this allele was introduced in a later nomenclature version (v3440) but maps to the same G group found in the test haplotype (A*02:01:01G)
            const string alleleFromLaterHlaVersion = "02:01:01:170";
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, LocusPosition.One, alleleFromLaterHlaVersion)
                .Build();

            var haplotypes = new List<LociInfo<string>> { HaplotypeBuilder1.Build(), HaplotypeBuilder2.Build() };

            Expander.Invoking(async service => await service.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                {
                    GGroup = haplotypes,
                    PGroup = haplotypes,
                    SmallGGroup = haplotypes
                }
            })).Should().Throw<HlaMetadataDictionaryException>();
        }
    }
}
