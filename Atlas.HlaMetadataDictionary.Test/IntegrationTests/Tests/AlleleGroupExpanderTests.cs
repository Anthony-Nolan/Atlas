using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    /// <summary>
    /// Fixture relies on a file-backed HlaMetadataDictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class AlleleGroupExpanderTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private IAlleleGroupExpander alleleGroupExpander;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                alleleGroupExpander = DependencyInjection.DependencyInjection.Provider.GetService<IAlleleGroupExpander>();
            });
        }

        [Test]
        public async Task ExpandAlleleGroup_WhenPGroup_ReturnsExpectedAlleles()
        {
            const string pGroup = "01:88P";
            var expectedAlleles = new List<string> { "01:88:01", "01:88:02", "01:88:03" };

            var alleles = await alleleGroupExpander.ExpandAlleleGroup(DefaultLocus, pGroup, HlaVersion);

            alleles.Should().BeEquivalentTo(expectedAlleles);
        }

        [Test]
        public async Task ExpandAlleleGroup_WhenGGroup_ReturnsExpectedAlleles()
        {
            const string gGroup = "02:02:01G";
            var expectedAlleles = new List<string> { "02:02:01:01", "02:02:01:02", "02:02:01:03", "02:02:01:04", "02:717" };

            var alleles = await alleleGroupExpander.ExpandAlleleGroup(DefaultLocus, gGroup, HlaVersion);

            alleles.Should().BeEquivalentTo(expectedAlleles);
        }

        [Test]
        public async Task ExpandAlleleGroup_WhenSmallGGroup_ReturnsExpectedAlleles()
        {
            const string gGroup = "02:04g";
            var expectedAlleles = new List<string> { "02:04", "02:664", "02:710N" };

            var alleles = await alleleGroupExpander.ExpandAlleleGroup(DefaultLocus, gGroup, HlaVersion);

            alleles.Should().BeEquivalentTo(expectedAlleles);
        }

        [TestCase("1")]
        [TestCase("01:01")]
        [TestCase("01:01:01")]
        [TestCase("01:01/02")]
        [TestCase("01:01/02:01")]
        [TestCase("01:MAC")]
        [TestCase("01:XX")]
        public void ExpandAlleleGroup_WhenValidHlaIsNotAnAlleleGroup_ThrowsException(string lookupName)
        {
            alleleGroupExpander.Invoking(async service => await service.ExpandAlleleGroup(DefaultLocus, lookupName, HlaVersion))
                .Should().Throw<Exception>();
        }
    }
}
