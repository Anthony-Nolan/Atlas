using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using FluentAssertions;
using NUnit.Framework;
using NSubstitute;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class GenotypeMatcherTests
    {
        private IMatchCalculationService matchCalculationService;
        private IGenotypeMatcher genotypeMatcher;

        private const string HlaNomenclatureVersion = "test";

        private const string PatientLocus1 = "patientGenotype1";
        private const string PatientLocus2 = "patientGenotype2";
        private const string DonorLocus1 = "donorGenotype1";
        private const string DonorLocus2 = "donorGenotype2";

        private static readonly PhenotypeInfo<string> PatientGenotype1 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = PatientLocus1, Position2 = PatientLocus1}).Build();
        private static readonly PhenotypeInfo<string> PatientGenotype2 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = PatientLocus2, Position2 = PatientLocus2}).Build();
        private static readonly PhenotypeInfo<string> DonorGenotype1 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = DonorLocus1, Position2 = DonorLocus1}).Build();
        private static readonly PhenotypeInfo<string> DonorGenotype2 = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = DonorLocus2, Position2 = DonorLocus2}).Build();

        private static readonly GenotypeMatchDetails TenOutOfTenMatch = new GenotypeMatchDetails
            {MatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2}};

        private static readonly GenotypeMatchDetails Mismatch = new GenotypeMatchDetails
            {MatchCounts = new LociInfo<int?> {A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0}};

        [SetUp]
        public void Setup()
        {
            matchCalculationService = Substitute.For<IMatchCalculationService>();

            genotypeMatcher = new GenotypeMatcher(matchCalculationService);
        }

        [Test]
        public async Task PairsWithTenOutOfTenMatch_WhenAllCombinationsOfGenotypesAreTenOutOfTenMatch_ReturnsListOfDonorPatientPairs()
        {
            var patientGenotypes = new HashSet<PhenotypeInfo<string>>{PatientGenotype1, PatientGenotype2};
            var donorGenotypes = new HashSet<PhenotypeInfo<string>>{DonorGenotype1, DonorGenotype2};
            
            var expectedPatientDonorPairs = new List<UnorderedPair<PhenotypeInfo<string>>>
            {
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype1, Item2 = DonorGenotype1},
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype1, Item2 = DonorGenotype2},
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype2, Item2 = DonorGenotype1},
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype2, Item2 = DonorGenotype2}
            };

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(TenOutOfTenMatch);

            var actualPatientDonorPairs = await genotypeMatcher.PairsWithTenOutOfTenMatch(patientGenotypes, donorGenotypes, HlaNomenclatureVersion);

            actualPatientDonorPairs.ToList().Should().BeEquivalentTo(expectedPatientDonorPairs);
        }

        [Test]
        public async Task PairsWithTenOutOfTenMatch_WhenAllCombinationsOfGenotypesAreMismatch_ReturnsEmptyListOfDonorPatientPairs()
        {
            var patientGenotypes = new HashSet<PhenotypeInfo<string>> { PatientGenotype1, PatientGenotype2 };
            var donorGenotypes = new HashSet<PhenotypeInfo<string>> { DonorGenotype1, DonorGenotype2 };

            var expectedPatientDonorPairs = new List<UnorderedPair<PhenotypeInfo<string>>>();

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(Mismatch);

            var actualPatientDonorPairs = await genotypeMatcher.PairsWithTenOutOfTenMatch(patientGenotypes, donorGenotypes, HlaNomenclatureVersion);

            actualPatientDonorPairs.ToList().Should().BeEquivalentTo(expectedPatientDonorPairs);
        }

        [Test]
        public async Task PairsWithTenOutOfTenMatch_WhenMixtureOfMismatchesAndTenOutOfTenMatches_ReturnsListOfMatchingDonorPatientPairs()
        {
            var patientGenotypes = new HashSet<PhenotypeInfo<string>> { PatientGenotype1, PatientGenotype2 };
            var donorGenotypes = new HashSet<PhenotypeInfo<string>> { DonorGenotype1, DonorGenotype2 };

            var expectedPatientDonorPairs = new List<UnorderedPair<PhenotypeInfo<string>>>
            {
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype1, Item2 = DonorGenotype1},
                new UnorderedPair<PhenotypeInfo<string>>{Item1 = PatientGenotype2, Item2 = DonorGenotype2}
            };

            matchCalculationService.MatchAtPGroupLevel(PatientGenotype1, DonorGenotype1, Arg.Any<string>()).Returns(TenOutOfTenMatch);
            matchCalculationService.MatchAtPGroupLevel(PatientGenotype2, DonorGenotype2, Arg.Any<string>()).Returns(TenOutOfTenMatch);
            matchCalculationService.MatchAtPGroupLevel(PatientGenotype1, DonorGenotype2, Arg.Any<string>()).Returns(Mismatch);
            matchCalculationService.MatchAtPGroupLevel(PatientGenotype2, DonorGenotype1, Arg.Any<string>()).Returns(Mismatch);

            var actualPatientDonorPairs = await genotypeMatcher.PairsWithTenOutOfTenMatch(patientGenotypes, donorGenotypes, HlaNomenclatureVersion);

            actualPatientDonorPairs.ToList().Should().BeEquivalentTo(expectedPatientDonorPairs);
        }


        [TestCase(4, 1, 4)]
        [TestCase(3, 2, 6)]
        [TestCase(2, 3, 6)]
        [TestCase(1, 4, 4)]
        public async Task PairsWithTenOutOfTenMatch_CalculatesMatchCountsForEachPatientDonorGenotypePair(
            int numberOfDonorGenotypes,
            int numberOfPatientGenotypes,
            int numberOfPossibleCombinations)
        {
            var patientGenotypes = Enumerable.Range(1, numberOfPatientGenotypes).Select(i => new PhenotypeInfo<string>($"patient${i}")).ToHashSet();
            var donorGenotypes = Enumerable.Range(1, numberOfDonorGenotypes).Select(i => new PhenotypeInfo<string>($"donor${i}")).ToHashSet();

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(TenOutOfTenMatch);

            await genotypeMatcher.PairsWithTenOutOfTenMatch(patientGenotypes, donorGenotypes, HlaNomenclatureVersion);

            await matchCalculationService.Received(numberOfPossibleCombinations)
                .MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>());
        }
    }
}
