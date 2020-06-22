using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests;
using Atlas.MatchPrediction.Services.MatchCalculation;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchCalculation
{
    [TestFixture]
    public class MatchCalculationTests
    {
        private IMatchCalculationService matchCalculationService;

        private const string HlaNomenclatureVersion = Constants.SnapshotHlaNomenclatureVersion;

        private const string A1 = "02:09";
        private const string A2 = "02:66";
        private const string B1 = "08:182";
        private const string B2 = "15:146";
        private const string C1 = "01:03";
        private const string C2 = "03:05";
        private const string Dqb11 = "03:09";
        private const string Dqb12 = "02:04";
        private const string Drb11 = "03:124";
        private const string Drb12 = "11:129";

        private static readonly LociInfo<int> TenOutOfTenMatch = new LociInfo<int> {A = 2, B = 2, C = 2, Dqb1 = 2, Drb1 = 2};

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            matchCalculationService =
                DependencyInjection.DependencyInjection.Provider.GetService<IMatchCalculationService>();
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenIdenticalGenotypes_IsTenOutOfTenMatch()
        {
            var matchCount =
                await matchCalculationService.MatchAtPGroupLevel(NewGenotype, NewGenotype, HlaNomenclatureVersion);

            matchCount.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenGenotypesWithDifferentAllelesWithSameGGroup_IsTenOutOfTenMatch()
        {
            var matchCount =
                await matchCalculationService.MatchAtPGroupLevel(NewGenotype, NewGenotype, HlaNomenclatureVersion);

            matchCount.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenGenotypesWithDifferentAllelesWithSamePGroup_IsTenOutOfTenMatch()
        {
            var donorGenotype = NewGenotype.With(g => g.A, new LocusInfo<string> {Position1 = "02:09, Position2 = A2});

            var matchCount =
                await matchCalculationService.MatchAtPGroupLevel(NewGenotype, NewGenotype, HlaNomenclatureVersion);

            matchCount.Should().BeEquivalentTo(TenOutOfTenMatch);
        }



        private static Builder<PhenotypeInfo<string>> NewGenotype => Builder<PhenotypeInfo<string>>.New
            .With(g => g.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
            .With(g => g.B, new LocusInfo<string> {Position1 = B1, Position2 = B2})
            .With(g => g.C, new LocusInfo<string> {Position1 = C1, Position2 = C2})
            .With(g => g.Dpb1, new LocusInfo<string> {Position1 = null, Position2 = null})
            .With(g => g.Dqb1, new LocusInfo<string> {Position1 = Dqb11, Position2 = Dqb12})
            .With(g => g.Drb1, new LocusInfo<string> {Position1 = Drb11, Position2 = Drb12});
    }
}