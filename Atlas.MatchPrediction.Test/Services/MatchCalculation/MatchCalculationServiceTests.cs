using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
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

        private static readonly ISet<Locus> AllowedLoci = new HashSet<Locus> {Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1};

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

        [TestCase(5)]
        [TestCase(4)]
        [TestCase(3)]
        public async Task MatchAtPGroupLevel_MatchCountIsCalledPerLocus(int numberOfLoci)
        {
            await matchCalculationService.MatchAtPGroupLevel(default, default, default, AllowedLoci.Take(numberOfLoci).ToHashSet());

            locusMatchCalculator.Received(numberOfLoci)
                .MatchCount(Arg.Any<LocusInfo<IEnumerable<string>>>(), Arg.Any<LocusInfo<IEnumerable<string>>>());
        }

        [TestCase(2, 10, 5)]
        [TestCase(1, 5, 5)]
        [TestCase(0, 0, 5)]
        [TestCase(2, 8, 4)]
        [TestCase(1, 4, 4)]
        [TestCase(0, 0, 4)]
        [TestCase(2, 6, 3)]
        [TestCase(1, 3, 3)]
        [TestCase(0, 0, 3)]
        public async Task MatchAtPGroupLevel_MatchCountsShouldAddUpToExpectedNumber(int perLocusMatchCount, int expectedTotalMatchCount, int numberOfLoci)
        {
            locusMatchCalculator.MatchCount(Arg.Any<LocusInfo<IEnumerable<string>>>(), Arg.Any<LocusInfo<IEnumerable<string>>>()).Returns(perLocusMatchCount);

            var match = await matchCalculationService.MatchAtPGroupLevel(default, default, default, AllowedLoci.Take(numberOfLoci).ToHashSet());

            match.MatchCount.Should().Be(expectedTotalMatchCount);
        }
    }
}