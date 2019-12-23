using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Locus = Nova.SearchAlgorithm.Common.Models.Locus;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.MatchingDictionary
{
    /// <summary>
    /// Fixture relies on a file-backed matching dictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class HlaMatchingLookupLookupTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const MolecularLocusType DefaultMolecularLocusType = MolecularLocusType.A;
        private const string CacheKey = "NmdpCodeLookup_A";

        private IHlaMatchingLookupService lookupService;
        private IHlaServiceClient hlaServiceClient;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            lookupService = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMatchingLookupService>();
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
        public async Task GetHlaLookupResult_WhenNmdpCode_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";

            // NMDP code value does not matter, but does need to conform to the expected pattern
            const string nmdpCode = "99:CODE";
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(DefaultMolecularLocusType, nmdpCode)
                .Returns(new List<string> { firstAllele, secondAllele });

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, nmdpCode, null);

            result.MatchingPGroups.ShouldBeEquivalentTo(new[] { firstAllele, secondAllele });
        }

        [Test]
        public async Task GetHlaLookupResult_WhenAlleleStringOfNames_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null);

            result.MatchingPGroups.ShouldBeEquivalentTo(new[] { firstAllele, secondAllele });
        }

        [Test]
        public async Task GetHlaLookupResult_WhenAlleleStringOfSubtypes_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = "01:133/158";

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null);

            result.MatchingPGroups.ShouldBeEquivalentTo(new[] { firstAllele, secondAllele });
        }
    }
}
