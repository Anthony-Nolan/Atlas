using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
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
        private IUnambiguousGenotypeExpander unambiguousGenotypeExpander;
        private IHaplotypeFrequenciesRepository frequencyRepository;
        private IGenotypeLikelihoodCalculator genotypeLikelihoodCalculator;
        private IGenotypeAlleleTruncater alleleTruncater;

        [SetUp]
        public void SetUp()
        {
            unambiguousGenotypeExpander = Substitute.For<IUnambiguousGenotypeExpander>();
            frequencyRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
            genotypeLikelihoodCalculator = Substitute.For<IGenotypeLikelihoodCalculator>();
            alleleTruncater = Substitute.For<IGenotypeAlleleTruncater>();

            alleleTruncater.TruncateGenotypeAlleles(Arg.Any<PhenotypeInfo<string>>())
                .Returns(arg => arg[0]);
            
            unambiguousGenotypeExpander.ExpandGenotype(Arg.Any<PhenotypeInfo<string>>())
                .Returns(new ExpandedGenotype {Diplotypes = new List<Diplotype> {DiplotypeBuilder.New.Build()}});

            frequencyRepository.GetHaplotypeFrequencies(Arg.Any<IEnumerable<LociInfo<string>>>(), Arg.Any<int>())
                .Returns(new Dictionary<LociInfo<string>, decimal> {{new LociInfo<string>(), 0}});

            genotypeLikelihoodCalculator.CalculateLikelihood(Arg.Any<ExpandedGenotype>())
                .Returns(0);

            genotypeLikelihoodService = new GenotypeLikelihoodService(
                frequencyRepository,
                unambiguousGenotypeExpander,
                genotypeLikelihoodCalculator
            );
        }

        [Test]
        public async Task CalculateLikelihood_FrequencyRepositoryIsCalledOnce([Values(16, 8, 4, 2, 1)] int numberOfDiplotypes)
        {
            unambiguousGenotypeExpander.ExpandGenotype(Arg.Any<PhenotypeInfo<string>>())
                .Returns(new ExpandedGenotype {Diplotypes = DiplotypeBuilder.New.Build(numberOfDiplotypes).ToList()});

            await genotypeLikelihoodService.CalculateLikelihood(new PhenotypeInfo<string>(), new HaplotypeFrequencySet());

            await frequencyRepository.Received(1)
                .GetHaplotypeFrequencies(Arg.Any<IEnumerable<LociInfo<string>>>(), Arg.Any<int>());
        }

        [Test]
        public async Task CalculateLikelihood_LikelihoodCalculatorIsCalledOnce()
        {
            await genotypeLikelihoodService.CalculateLikelihood(new PhenotypeInfo<string>(), new HaplotypeFrequencySet());

            genotypeLikelihoodCalculator.Received(1)
                .CalculateLikelihood(Arg.Any<ExpandedGenotype>());
        }
    }
}