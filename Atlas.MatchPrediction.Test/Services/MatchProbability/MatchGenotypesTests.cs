using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using FluentAssertions;
using NUnit.Framework;
using NSubstitute;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class MatchGenotypesTests
    {
        private IMatchCalculationService matchCalculationService;
        private IMatchGenotypes matchGenotypes;

        private const string HlaNomenclatureVersion = "test";

        private static readonly PhenotypeInfo<string> PatientGenotype1 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> { Position1 = "patientGenotype1", Position2 = "patientGenotype1" }).Build();
        private static readonly PhenotypeInfo<string> PatientGenotype2 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> { Position1 = "patientGenotype2", Position2 = "patientGenotype2" }).Build();
        private static readonly PhenotypeInfo<string> DonorGenotype1 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> { Position1 = "donorGenotype1", Position2 = "donorGenotype1" }).Build();
        private static readonly PhenotypeInfo<string> DonorGenotype2 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> { Position1 = "donorGenotype2", Position2 = "donorGenotype2" }).Build();

        private static readonly LociInfo<int?> TenOutOfTenMatch = new LociInfo<int?>
            { A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2 };


        private static readonly LociInfo<int?> Mismatch = new LociInfo<int?>
            { A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0 };

        [SetUp]
        public void Setup()
        {
            matchCalculationService = Substitute.For<IMatchCalculationService>();

            matchGenotypes = new MatchGenotypes(matchCalculationService);
        }

        [Test]
        public async Task PairsWithAlleleLevelMatch_WhenTenOutOfTenMatch_ReturnsListOfDonorPatientPairs()
        {
            var patientGenotypes = new List<PhenotypeInfo<string>>{PatientGenotype1, PatientGenotype2};
            var donorGenotypes = new List<PhenotypeInfo<string>>{DonorGenotype1, DonorGenotype2};
            
            var expectedPatientDonorPairs = new List<UnorderedPair<PhenotypeInfo<string>>>
            {
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype1, Item2 = DonorGenotype1},
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype1, Item2 = DonorGenotype2},
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype2, Item2 = DonorGenotype1},
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype2, Item2 = DonorGenotype2}
            };

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(TenOutOfTenMatch);

            var actualPatientDonorPairs = await matchGenotypes.PairsWithAlleleLevelMatch(patientGenotypes, donorGenotypes, HlaNomenclatureVersion);

            actualPatientDonorPairs.ToList().Should().BeEquivalentTo(expectedPatientDonorPairs);
        }

        [Test]
        public async Task PairsWithAlleleLevelMatch_WhenMismatch_ReturnsEmptyListOfDonorPatientPairs()
        {
            var patientGenotypes = new List<PhenotypeInfo<string>> { PatientGenotype1, PatientGenotype2 };
            var donorGenotypes = new List<PhenotypeInfo<string>> { DonorGenotype1, DonorGenotype2 };

            var expectedPatientDonorPairs = new List<UnorderedPair<PhenotypeInfo<string>>>();

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(Mismatch);

            var actualPatientDonorPairs = await matchGenotypes.PairsWithAlleleLevelMatch(patientGenotypes, donorGenotypes, HlaNomenclatureVersion);

            actualPatientDonorPairs.ToList().Should().BeEquivalentTo(expectedPatientDonorPairs);
        }


        [TestCase(4,1,4)]
        [TestCase(3, 2, 6)]
        [TestCase(2, 3, 6)]
        [TestCase(1, 4, 4)]
        public async Task PairsWithAlleleLevelMatch_MatchCalculationServiceIsCalledTheExpectedAmountOfTimes(
            int numberOfDonorGenotypes,
            int numberOfPatientGenotypes,
            int numberOfPossibleCombinations)
        {
            var patientGenotypes = Enumerable.Range(1, numberOfPatientGenotypes).Select(i => new PhenotypeInfo<string>()).ToList();
            var donorGenotypes = Enumerable.Range(1, numberOfDonorGenotypes).Select(i => new PhenotypeInfo<string>()).ToList();

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(TenOutOfTenMatch);

            await matchGenotypes.PairsWithAlleleLevelMatch(patientGenotypes, donorGenotypes, HlaNomenclatureVersion);


            await matchCalculationService.Received(numberOfPossibleCombinations)
                .MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>());
        }
    }
}
