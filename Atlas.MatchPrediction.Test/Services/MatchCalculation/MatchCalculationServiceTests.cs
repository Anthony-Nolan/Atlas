using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.MatchPrediction.Services;
using Atlas.MatchPrediction.Services.MatchCalculation;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.MatchCalculation
{
    [TestFixture]
    internal class MatchCalculationServiceTests
    {
        private ILocusMatchCalculator locusMatchCalculator;
        private ILocusHlaConverter locusHlaConverter;

        private IMatchCalculationService matchCalculationService;

        [SetUp]
        public void SetUp()
        {
            locusMatchCalculator = Substitute.For<ILocusMatchCalculator>();
            locusHlaConverter = Substitute.For<ILocusHlaConverter>();

            locusHlaConverter.ConvertHla(default, default, default, default)
                .ReturnsForAnyArgs(new PhenotypeInfo<IReadOnlyCollection<string>>(new List<string> {"hla"}));

            locusMatchCalculator.MatchCount(Arg.Any<LocusInfo<IEnumerable<string>>>(), Arg.Any<LocusInfo<IEnumerable<string>>>()).Returns(2);

            matchCalculationService = new MatchCalculationService(locusHlaConverter, locusMatchCalculator);
        }

        [Test]
        public async Task MatchAtPGroupLevel_MatchCountIsCalledPerLocus()
        {
            await matchCalculationService.MatchAtPGroupLevel(default, default, default, default, default);

            locusMatchCalculator.Received(5)
                .MatchCount(Arg.Any<LocusInfo<IEnumerable<string>>>(), Arg.Any<LocusInfo<IEnumerable<string>>>());
        }

        [TestCase(2, 10)]
        [TestCase(1, 5)]
        [TestCase(0, 0)]
        public async Task MatchAtPGroupLevel_MatchCountsShouldAddUpToExpectedNumber(int perLocusMatchCount, int expectedTotalMatchCount)
        {
            locusMatchCalculator.MatchCount(Arg.Any<LocusInfo<IEnumerable<string>>>(), Arg.Any<LocusInfo<IEnumerable<string>>>()).Returns(perLocusMatchCount);

            var match = await matchCalculationService.MatchAtPGroupLevel(default, default, default, default, default);

            // Not including Dpb1 as it's not included in match prediction
            var actualTotal = match.MatchCounts.A + match.MatchCounts.B + match.MatchCounts.C + match.MatchCounts.Dqb1 + match.MatchCounts.Drb1;
            actualTotal.Should().Be(expectedTotalMatchCount);
        }
    }
}