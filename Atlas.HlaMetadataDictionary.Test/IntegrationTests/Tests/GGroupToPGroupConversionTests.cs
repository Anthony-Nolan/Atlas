using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    [TestFixture]
    public class GGroupToPGroupConversionTests
    {
        private IHlaMetadataDictionary hlaMetadataDictionary;
        
        [SetUp]
        public void SetUp()
        {
            var factory = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMetadataDictionaryFactory>();
            hlaMetadataDictionary = factory.BuildDictionary(Constants.SnapshotHlaNomenclatureVersion);
        }
        
        [TestCase(Locus.A, "01:01:01G", "01:01P")]
        [TestCase(Locus.B, "08:01:01G", "08:01P")]
        [TestCase(Locus.C, "07:01:01G", "07:01P")]
        [TestCase(Locus.Dqb1, "02:01:01G", "02:01P")]
        [TestCase(Locus.Drb1, "03:01:01G", "03:01P")]
        public async Task GetSinglePGroupForGGroup_WithMatchingPGroup_ReturnsPGroup(Locus locus, string gGroup, string expectedPGroup)
        {
            var pGroup = await hlaMetadataDictionary.GetSinglePGroupForGGroup(locus, gGroup);

            pGroup.Should().Be(expectedPGroup);
        }
        
        [TestCase(Locus.A, "01:11N")]
        [TestCase(Locus.B, "08:01N")]
        [TestCase(Locus.C, "07:01N")]
        [TestCase(Locus.Dqb1, "02:01N")]
        [TestCase(Locus.Drb1, "03:02N")]
        public async Task GetSinglePGroupForGGroup_WithNoMatchingPGroup_ReturnsNull(Locus locus, string gGroup)
        {
            var pGroup = await hlaMetadataDictionary.GetSinglePGroupForGGroup(locus, gGroup);

            pGroup.Should().Be(null);
        }
    }
}