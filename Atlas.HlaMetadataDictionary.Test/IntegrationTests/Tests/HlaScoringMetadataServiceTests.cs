using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    /// <summary>
    /// Fixture relies on a file-backed HlaMetadataDictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class HlaScoringMetadataServiceTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string CacheKey = "NmdpCodeLookup_A";
        private const string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private IHlaScoringMetadataService metadataService;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<IHlaScoringMetadataService>();
                appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
            });
        }

        [TearDown]
        public void TearDown()
        {
            // clear MAC allele mappings between tests
            appCache.Remove(CacheKey);
        }

        [Test]
        public async Task GetHlaMetadata_WhenSerology_ReturnsSerologyScoringInfo()
        {
            const Locus serologyLocus = Locus.B;
            const string serologyName = "82";
            var expectedSerologies = new List<SerologyEntry> { new SerologyEntry("82", SerologySubtype.NotSplit, true) };
            var expectedGGroups = new[] { "82:01:01G", "82:02:01G", "82:02:02", "82:03" };
            var expectedPGroups = new[] { "82:01P", "82:02P", "82:03" };

            var result = await metadataService.GetHlaMetadata(serologyLocus, serologyName, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<SerologyScoringInfo>();
            ((SerologyScoringInfo)scoringInfo).MatchingSerologies.Should().BeEquivalentTo(expectedSerologies);
            ((SerologyScoringInfo)scoringInfo).MatchingGGroups.Should().BeEquivalentTo(expectedGGroups);
            ((SerologyScoringInfo)scoringInfo).MatchingPGroups.Should().BeEquivalentTo(expectedPGroups);
        }

        [Test]
        public async Task GetHlaMetadata_WhenAlleleNameMapsToMultipleAlleles_ReturnsScoringInfoForEachAllele()
        {
            // Allele name truncated to its 2-field variant
            const string truncatedAlleleName = "01:03";
            const int expectedAlleleCount = 2;

            var result = await metadataService.GetHlaMetadata(DefaultLocus, truncatedAlleleName, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<MultipleAlleleScoringInfo>();
            ((MultipleAlleleScoringInfo)scoringInfo).AlleleScoringInfos.Should().HaveCount(expectedAlleleCount);
        }

        // TODO: ATLAS-454: Confirm scoring metadata lookup strategy for P groups
        [Test]
        public async Task GetHlaMetadata_WhenPGroup_ReturnsConsolidatedScoringInfo()
        {
            const string pGroup = "01:09P";
            var expectedGGroups = new List<string> { "01:09:01G", "01:09:02" };
            var expectedSerologies = new List<SerologyEntry> { new SerologyEntry("1", SerologySubtype.NotSplit, true) };

            var result = await metadataService.GetHlaMetadata(DefaultLocus, pGroup, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.Should().BeEquivalentTo(pGroup);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.Should().BeEquivalentTo(expectedGGroups);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingSerologies.Should().BeEquivalentTo(expectedSerologies);
        }

        // TODO: ATLAS-454: Confirm scoring metadata lookup strategy for G groups
        [Test]
        public async Task GetHlaMetadata_WhenGGroup_ReturnsConsolidatedScoringInfo()
        {
            const string gGroup = "01:01:01G";
            const string expectedPGroup = "01:01P";
            var expectedSerologies = new List<SerologyEntry> { new SerologyEntry("1", SerologySubtype.NotSplit, true) };

            var result = await metadataService.GetHlaMetadata(DefaultLocus, gGroup, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.Should().BeEquivalentTo(expectedPGroup);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.Should().BeEquivalentTo(gGroup);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingSerologies.Should().BeEquivalentTo(expectedSerologies);
        }

        [Test]
        public async Task GetHlaMetadata_WhenMac_ReturnsConsolidatedScoringInfo()
        {
            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories.LargeMacDictionary.csv
            // XYZ => 133/158
            const string macWithFirstField = "01:XYZ";

            // each allele maps to a G and P group of the same name
            var alleles = new[] { "01:133", "01:158" };

            const string expectedSerology = "1";

            var result = await metadataService.GetHlaMetadata(DefaultLocus, macWithFirstField, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.Should().BeEquivalentTo(alleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.Should().BeEquivalentTo(alleles);
            scoringInfo.MatchingSerologies.Select(s => s.Name).Should().BeEquivalentTo(expectedSerology);
        }

        [Test]
        public async Task GetHlaMetadata_WhenMacIncludesNullAllele_OnlyReturnsScoringInfoForExpressingAlleles()
        {
            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories.LargeMacDictionary.csv
            // ZZZ => 04N/133/158
            const string macWithFirstField = "01:ZZZ";

            // expressing alleles maps to a G and P group of the same name
            var expressingAlleles = new[] { "01:133", "01:158" };

            var result = await metadataService.GetHlaMetadata(DefaultLocus, macWithFirstField, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.Should().BeEquivalentTo(expressingAlleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.Should().BeEquivalentTo(expressingAlleles);
        }

        [Test]
        public async Task GetHlaMetadata_WhenAlleleStringOfNames_ReturnsConsolidatedScoringInfo()
        {
            // each allele maps to a G and P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = firstAllele + "/" + secondAllele;
            
            var alleles = new[] { firstAllele, secondAllele };
            const string expectedSerology = "1";

            var result = await metadataService.GetHlaMetadata(DefaultLocus, alleleString, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.Should().BeEquivalentTo(alleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.Should().BeEquivalentTo(alleles);
            scoringInfo.MatchingSerologies.Select(s => s.Name).Should().BeEquivalentTo(expectedSerology);
        }

        [Test]
        public async Task GetHlaMetadata_WhenAlleleStringOfSubtypes_ReturnsConsolidatedScoringInfo()
        {
            // each allele maps to a G and P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = "01:133/158";

            var alleles = new[] { firstAllele, secondAllele };
            const string expectedSerology = "1";

            var result = await metadataService.GetHlaMetadata(DefaultLocus, alleleString, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.Should().BeEquivalentTo(alleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.Should().BeEquivalentTo(alleles);
            scoringInfo.MatchingSerologies.Select(s => s.Name).Should().BeEquivalentTo(expectedSerology);
        }

        [Test]
        public async Task GetHlaMetadata_WhenAlleleStringIncludesNullAllele_OnlyReturnsScoringInfoForExpressingAlleles()
        {
            // expressing alleles maps to a G and P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string nullAllele = "01:04:01:01N";
            const string alleleString = firstAllele + "/" + secondAllele + "/" + nullAllele;
            var expressingAlleles = new[] { firstAllele, secondAllele };

            var result = await metadataService.GetHlaMetadata(DefaultLocus, alleleString, HlaVersion);

            var scoringInfo = result.HlaScoringInfo;
            scoringInfo.Should().BeOfType<ConsolidatedMolecularScoringInfo>();
            scoringInfo.MatchingPGroups.Should().BeEquivalentTo(expressingAlleles);
            ((ConsolidatedMolecularScoringInfo)scoringInfo).MatchingGGroups.Should().BeEquivalentTo(expressingAlleles);
        }

        /// <summary>
        /// Regression test
        /// </summary>
        [Test]
        public async Task GetHlaLookupResult_WhenMacExpandsToOnly3or4FieldAlleles_ReturnsSerologyInScoringInfo()
        {
            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories.LargeMacDictionary.csv
            // 02:AB => 02:01/02
            const string nmdpCode = "02:AB";
            var expectedSerologies = new[] { "2", "210", "203" };

            var result = await metadataService.GetHlaMetadata(DefaultLocus, nmdpCode, HlaVersion);

            result.HlaScoringInfo.MatchingSerologies.Select(s => s.Name).Should().BeEquivalentTo(expectedSerologies);
        }

        /// <summary>
        /// Regression test
        /// </summary>
        [TestCase("02:01/02")]
        [TestCase("02:01/02:02")]
        public async Task GetHlaLookupResult_WhenAlleleStringExpandsToOnly3or4FieldAlleles_ReturnsSerologyInScoringInfo(string alleleString)
        {
            var expectedSerologies = new[] { "2", "210", "203" };

            var result = await metadataService.GetHlaMetadata(DefaultLocus, alleleString, HlaVersion);

            result.HlaScoringInfo.MatchingSerologies.Select(s => s.Name).Should().BeEquivalentTo(expectedSerologies);
        }
    }
}