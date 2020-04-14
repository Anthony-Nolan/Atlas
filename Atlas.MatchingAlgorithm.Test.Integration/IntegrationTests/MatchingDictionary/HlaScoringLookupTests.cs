using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Locus = Atlas.MatchingAlgorithm.Common.Models.Locus;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.MatchingDictionary
{
    /// <summary>
    /// Fixture relies on a file-backed matching dictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class HlaScoringLookupLookupTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const MolecularLocusType DefaultMolecularLocusType = MolecularLocusType.A;
        private const string CacheKey = "NmdpCodeLookup_A";

        private IHlaScoringLookupService lookupService;
        private IHlaServiceClient hlaServiceClient;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            lookupService = DependencyInjection.DependencyInjection.Provider.GetService<IHlaScoringLookupService>();
            hlaServiceClient = DependencyInjection.DependencyInjection.Provider.GetService<IHlaServiceClient>();
            appCache = DependencyInjection.DependencyInjection.Provider.GetService<IAppCache>();
        }

        [SetUp]
        public void SetUp()
        {
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(DefaultMolecularLocusType, Arg.Any<string>())
                .Returns(new List<string>());

            // clear NMDP code allele mappings between tests
            appCache.Remove(CacheKey);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenAlleleNameMapsToMultipleAlleles_ReturnsScoringInfoForEachAllele()
        {
            // Allele name truncated to its 2-field variant
            const string truncatedAlleleName = "01:03";
            const int expectedAlleleCount = 2;

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, truncatedAlleleName, null);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<MultipleAlleleScoringInfo>();
            ((MultipleAlleleScoringInfo)scoringInfo).AlleleScoringInfos.Count().Should().Be(expectedAlleleCount);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenNmdpCode_ReturnsConsolidatedScoringInfo()
        {
            // each allele maps to a G and P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            var alleles = new[] { firstAllele, secondAllele };

            // NMDP code value does not matter, but does need to conform to the expected pattern
            const string nmdpCode = "99:CODE";
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(DefaultMolecularLocusType, nmdpCode)
                .Returns(alleles.ToList());

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, nmdpCode, null);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.ShouldBeEquivalentTo(alleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.ShouldBeEquivalentTo(alleles);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenNmdpCodeIncludesNullAllele_OnlyReturnsScoringInfoForExpressingAlleles()
        {
            // expressing alleles maps to a G and P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string nullAllele = "01:04:01:01N";

            var expressingAlleles = new[] { firstAllele, secondAllele };
            var allAlleles = new List<string>(expressingAlleles) { nullAllele };

            // NMDP code value does not matter, but does need to conform to the expected pattern
            const string nmdpCode = "99:CODE";
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(DefaultMolecularLocusType, nmdpCode)
                .Returns(allAlleles);

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, nmdpCode, null);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.ShouldBeEquivalentTo(expressingAlleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.ShouldBeEquivalentTo(expressingAlleles);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenAlleleStringOfNames_ReturnsConsolidatedScoringInfo()
        {
            // each allele maps to a G and P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = firstAllele + "/" + secondAllele;
            var alleles = new[] { firstAllele, secondAllele };

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.ShouldBeEquivalentTo(alleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.ShouldBeEquivalentTo(alleles);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenAlleleStringOfSubtypes_ReturnsConsolidatedScoringInfo()
        {
            // each allele maps to a G and P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = "01:133/158";
            var alleles = new[] { firstAllele, secondAllele };

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.ShouldBeEquivalentTo(alleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.ShouldBeEquivalentTo(alleles);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenAlleleStringIncludesNullAllele_OnlyReturnsScoringInfoForExpressingAlleles()
        {
            // expressing alleles maps to a G and P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string nullAllele = "01:04:01:01N";
            const string alleleString = firstAllele + "/" + secondAllele + "/" + nullAllele;
            var expressingAlleles = new[] { firstAllele, secondAllele };

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.ShouldBeEquivalentTo(expressingAlleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.ShouldBeEquivalentTo(expressingAlleles);
        }
    }
}
