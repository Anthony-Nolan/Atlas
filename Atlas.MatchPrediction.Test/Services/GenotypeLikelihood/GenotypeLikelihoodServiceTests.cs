using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    public class GenotypeLikelihoodServiceTests
    {
        private IGenotypeLikelihoodService genotypeLikelihoodService;
        private IGenotypeImputer genotypeImputer;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequenciesRepository frequencyRepository;
        private IGenotypeLikelihoodCalculator genotypeLikelihoodCalculator;

        [SetUp]
        public void SetUp()
        {
            genotypeImputer = Substitute.For<IGenotypeImputer>();
            frequencyRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
            setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
            genotypeLikelihoodCalculator = Substitute.For<IGenotypeLikelihoodCalculator>();

            setRepository.GetActiveSet(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new HaplotypeFrequencySet {Id = 1});

            genotypeImputer.ImputeGenotype(Arg.Any<PhenotypeInfo<string>>())
                .Returns(new ImputedGenotype {Diplotypes = new List<Diplotype> {DiplotypeBuilder.New.Build()}});

            frequencyRepository.GetHaplotypeFrequencies(Arg.Any<IEnumerable<LociInfo<string>>>(), Arg.Any<int>())
                .Returns(new Dictionary<LociInfo<string>, decimal> {{new LociInfo<string>(), 0}});

            genotypeLikelihoodCalculator.CalculateLikelihood(Arg.Any<ImputedGenotype>())
                .Returns(0);

            genotypeLikelihoodService = new GenotypeLikelihoodService(
                setRepository,
                frequencyRepository,
                genotypeImputer,
                genotypeLikelihoodCalculator
            );
        }

        [Test]
        public async Task CalculateLikelihood_FrequencyRepositoryIsCalledOnce([Values(16, 8, 4, 2, 1)] int numberOfDiplotypes)
        {
            genotypeImputer.ImputeGenotype(Arg.Any<PhenotypeInfo<string>>())
                .Returns(new ImputedGenotype {Diplotypes = DiplotypeBuilder.New.Build(numberOfDiplotypes).ToList()});

            await genotypeLikelihoodService.CalculateLikelihood(new GenotypeLikelihoodInput());

            await frequencyRepository.Received(1)
                .GetHaplotypeFrequencies(Arg.Any<IEnumerable<LociInfo<string>>>(), Arg.Any<int>());
        }

        [Test]
        public async Task CalculateLikelihood_LikelihoodCalculatorIsCalledOnce()
        {
            await genotypeLikelihoodService.CalculateLikelihood(new GenotypeLikelihoodInput());

            genotypeLikelihoodCalculator.Received(1)
                .CalculateLikelihood(Arg.Any<ImputedGenotype>());
        }
    }
}