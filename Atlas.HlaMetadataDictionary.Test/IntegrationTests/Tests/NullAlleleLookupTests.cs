using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    [TestFixture]
    public class NullAlleleLookupTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private IHlaMatchingMetadataService metadataService;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMatchingMetadataService>();
            });
        }

        [Test]
        public async Task GetHlaMetadata_ForCombinedNullAlleleAndExpressingAllele_ReturnsNullAlleleData()
        {
            const string expressingAllele = "01:01";
            const string nullAllele = "01:01N";
            var hlaName = NullAlleleHandling.CombineAlleleNames(nullAllele, expressingAllele);

            var combinedResult = await metadataService.GetHlaMetadata(DefaultLocus, hlaName, HlaVersion);
            var nullOnlyResult = await metadataService.GetHlaMetadata(DefaultLocus, nullAllele, HlaVersion);

            combinedResult.MatchingPGroups.Should().BeEmpty();
            nullOnlyResult.MatchingPGroups.Should().BeEmpty();
            combinedResult.Should().BeEquivalentTo(nullOnlyResult);
        }
    }
}