using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    /// <summary>
    /// Fixture relies on a file-backed HlaMetadataDictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class Dpb1TceGroupMetadataServiceTests
    {
        private const Locus Dpb1MolecularLocusType = Locus.Dpb1;
        private const string CacheKey = "NmdpCodeLookup_Dpb1";

        private IDpb1TceGroupMetadataService metadataService;
        private IMacDictionary macDictionary;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<IDpb1TceGroupMetadataService>();
                macDictionary = DependencyInjection.DependencyInjection.Provider.GetService<IMacDictionary>();
                appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
            });
        }

        [SetUp]
        public void SetUp()
        {
            macDictionary
                .GetHlaFromMac(Arg.Any<string>())
                .Returns(new List<string>());

            // clear NMDP code allele mappings between tests
            appCache.Remove(CacheKey);
        }


        [Test]
        public async Task GetDpb1TceGroup_WhenNmdpCodeMapsToSingleTceGroup_ReturnsTceGroup()
        {
            // both alleles map to same TCE group
            const string firstAllele = "02:01";
            const string secondAllele = "02:02";
            const string tceGroup = "3";

            // MAC value does not matter, but does need to conform to the expected pattern
            const string macWithFirstField = "99:CODE";
            macDictionary
                .GetHlaFromMac(macWithFirstField)
                .Returns(new List<string> { firstAllele, secondAllele });

            macDictionary.GetHlaFromMac(default, default).Returns(new List<string> {firstAllele, secondAllele});

            var result = await metadataService.GetDpb1TceGroup(macWithFirstField, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenNmdpCodeMapsToMoreThanOneTceGroup_DoesNotReturnTceGroup()
        {
            // alleles map to different TCE groups
            const string firstAllele = "02:01";
            const string secondAllele = "03:01";

            // MAC value does not matter, but does need to conform to the expected pattern
            const string macWithFirstField = "99:CODE";
            macDictionary
                .GetHlaFromMac(macWithFirstField)
                .Returns(new List<string> { firstAllele, secondAllele });

            var result = await metadataService.GetDpb1TceGroup(macWithFirstField, null);

            result.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenAlleleStringOfNamesMapsToSingleTceGroup_ReturnsTceGroup()
        {
            // both alleles map to same TCE group
            const string firstAllele = "02:01";
            const string secondAllele = "02:02";
            const string tceGroup = "3";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await metadataService.GetDpb1TceGroup(alleleString, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenAlleleStringOfSubtypesMapsToSingleTceGroup_ReturnsTceGroup()
        {
            // both alleles map to same TCE group
            const string tceGroup = "3";
            const string alleleString = "02:01/02";

            var result = await metadataService.GetDpb1TceGroup(alleleString, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenAlleleStringOfNamesMapsToMultipleTceGroups_DoesNotReturnTceGroup()
        {
            // alleles map to different TCE group
            const string firstAllele = "02:01";
            const string secondAllele = "03:01";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await metadataService.GetDpb1TceGroup(alleleString, null);

            result.Should().BeNullOrEmpty();
        }
    }
}