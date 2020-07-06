using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
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
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequenciesRepository frequencyRepository;
        private IGenotypeLikelihoodCalculator genotypeLikelihoodCalculator;
        private IGenotypeAlleleTruncater alleleTruncater;

        [SetUp]
        public void SetUp()
        {
            unambiguousGenotypeExpander = Substitute.For<IUnambiguousGenotypeExpander>();
            frequencyRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
            setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
            genotypeLikelihoodCalculator = Substitute.For<IGenotypeLikelihoodCalculator>();
            alleleTruncater = Substitute.For<IGenotypeAlleleTruncater>();

            alleleTruncater.TruncateGenotypeAlleles(Arg.Any<PhenotypeInfo<string>>())
                .Returns(arg => arg[0]);

            setRepository.GetActiveSet(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new HaplotypeFrequencySet {Id = 1});

            unambiguousGenotypeExpander.ExpandGenotype(Arg.Any<PhenotypeInfo<string>>())
                .Returns(new ExpandedGenotype {Diplotypes = new List<Diplotype> {DiplotypeBuilder.New.Build()}});

            frequencyRepository.GetHaplotypeFrequencies(Arg.Any<IEnumerable<LociInfo<string>>>(), Arg.Any<int>())
                .Returns(new Dictionary<LociInfo<string>, decimal> {{new LociInfo<string>(), 0}});

            genotypeLikelihoodCalculator.CalculateLikelihood(Arg.Any<ExpandedGenotype>())
                .Returns(0);

            genotypeLikelihoodService = new GenotypeLikelihoodService(
                setRepository,
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

            await genotypeLikelihoodService.CalculateLikelihood(new PhenotypeInfo<string>(), new FrequencySetMetadata());

            await frequencyRepository.Received(1)
                .GetHaplotypeFrequencies(Arg.Any<IEnumerable<LociInfo<string>>>(), Arg.Any<int>());
        }

        [Test]
        public async Task CalculateLikelihood_LikelihoodCalculatorIsCalledOnce()
        {
            await genotypeLikelihoodService.CalculateLikelihood(new PhenotypeInfo<string>(), new FrequencySetMetadata());

            genotypeLikelihoodCalculator.Received(1)
                .CalculateLikelihood(Arg.Any<ExpandedGenotype>());
        }
    }
}