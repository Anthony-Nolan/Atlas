using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using FluentAssertions;
using NUnit.Framework;
using static Atlas.MatchPrediction.Test.Integration.Resources.Alleles.Alleles;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.PhenotypeExpansion
{
    [TestFixture]
    internal class FilteredExpansionWhereHlaExceptionsAreSuppressedTests : FilteredExpansionTestsBase
    {
        private const bool SuppressCompressedPhenotypeConversionExceptions = true;

        /// <inheritdoc />
        public FilteredExpansionWhereHlaExceptionsAreSuppressedTests() : base(SuppressCompressedPhenotypeConversionExceptions)
        {
        }

        // TODO: #637 - this test is expected to fail once a strategy is in place to handle valid alleles that are missing from
        // the HMD version being referenced during expansion; for now, such alleles will be deemed unrepresented in the HF set
        // even if they map to the G group listed in the haplotype.
        [Test]
        public async Task ExpandCompressedPhenotype_ContainsAlleleFromLaterHlaVersion_ReturnsEmptySet()
        {
            // this allele was introduced in a later nomenclature version (v3440) but maps to the same G group found in the test haplotype (A*02:01:01G)
            const string alleleFromLaterHlaVersion = "02:01:01:170";
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, LocusPosition.One, alleleFromLaterHlaVersion)
                .Build();

            var haplotypes = new List<LociInfo<string>> { HaplotypeBuilder1.Build(), HaplotypeBuilder2.Build() };

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
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
            });

            genotypes.Should().BeEmpty();
        }
    }
}
