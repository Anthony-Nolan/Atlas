using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
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

        [SetUp]
        public void Setup()
        {
            matchCalculationService = Substitute.For<IMatchCalculationService>();

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(new GenotypeMatchDetails());

            genotypeMatcher = new GenotypeMatcher(matchCalculationService);
        }

        [TestCase(5, 1, 5)]
        [TestCase(4, 2, 8)]
        [TestCase(3, 3, 9)]
        [TestCase(2, 4, 8)]
        [TestCase(1, 5, 5)]
        public async Task PairsWithMatch_CalculatesMatchCountsForEachPatientDonorGenotypePair(
            int numberOfDonorGenotypes,
            int numberOfPatientGenotypes,
            int numberOfPossibleCombinations)
        {
            var patientGenotypes = Enumerable.Range(1, numberOfPatientGenotypes).Select(i => new PhenotypeInfo<string>($"patient${i}")).ToHashSet();
            var donorGenotypes = Enumerable.Range(1, numberOfDonorGenotypes).Select(i => new PhenotypeInfo<string>($"donor${i}")).ToHashSet();

            await genotypeMatcher.PairsWithMatch(patientGenotypes, donorGenotypes, HlaNomenclatureVersion);

            await matchCalculationService.Received(numberOfPossibleCombinations)
                .MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>());
        }
    }
}
