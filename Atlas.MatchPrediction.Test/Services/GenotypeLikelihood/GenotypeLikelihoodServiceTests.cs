using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using NSubstitute;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    public class GenotypeLikelihoodServiceTests
    {
        private IGenotypeLikelihoodService genotypeLikelihoodService;
        private IUnambiguousGenotypeExpander unambiguousGenotypeExpander;
        private IHaplotypeFrequencyService frequencyService;
        private IGenotypeLikelihoodCalculator genotypeLikelihoodCalculator;
        private IGenotypeAlleleTruncater alleleTruncater;

        private readonly ISet<Locus> allLoci = LocusSettings.MatchPredictionLoci;

        [SetUp]
        public void SetUp()
        {
            unambiguousGenotypeExpander = Substitute.For<IUnambiguousGenotypeExpander>();
            frequencyService = Substitute.For<IHaplotypeFrequencyService>();
            genotypeLikelihoodCalculator = Substitute.For<IGenotypeLikelihoodCalculator>();
            alleleTruncater = Substitute.For<IGenotypeAlleleTruncater>();

            alleleTruncater.TruncateGenotypeAlleles(Arg.Any<PhenotypeInfo<string>>())
                .Returns(arg => arg[0]);

            unambiguousGenotypeExpander.ExpandGenotype(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<ISet<Locus>>())
                .Returns(new ExpandedGenotype {Diplotypes = new List<Diplotype> {new DiplotypeBuilder().Build()}});

            frequencyService.GetAllHaplotypeFrequencies(Arg.Any<int>())
                .Returns(new ConcurrentDictionary<LociInfo<string>, HaplotypeFrequency>(
                    new Dictionary<LociInfo<string>, HaplotypeFrequency> {{new LociInfo<string>(), HaplotypeFrequencyBuilder.New.Build()}})
                );

            genotypeLikelihoodCalculator.CalculateLikelihood(Arg.Any<ExpandedGenotype>()).Returns(0);

            genotypeLikelihoodService = new GenotypeLikelihoodService(unambiguousGenotypeExpander, genotypeLikelihoodCalculator, frequencyService);
        }

        [Test]
        public async Task CalculateLikelihood_FrequencyRepositoryIsCalledTwicePerDiplotype([Values(16, 8, 4, 2, 1)] int numberOfDiplotypes)
        {
            var diplotypes = Enumerable.Range(0, numberOfDiplotypes).Select(i => new DiplotypeBuilder().Build()).ToList();

            unambiguousGenotypeExpander.ExpandGenotype(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<ISet<Locus>>())
                .Returns(new ExpandedGenotype {Diplotypes = diplotypes});

            await genotypeLikelihoodService.CalculateLikelihoodForGenotype(new PhenotypeInfo<string>(), new HaplotypeFrequencySet(), allLoci);

            await frequencyService.ReceivedWithAnyArgs(2 * numberOfDiplotypes).GetFrequencyForHla(default, default, default);
        }

        [Test]
        public async Task CalculateLikelihood_LikelihoodCalculatorIsCalledOnce()
        {
            await genotypeLikelihoodService.CalculateLikelihoodForGenotype(new PhenotypeInfo<string>(), new HaplotypeFrequencySet(), allLoci);

            genotypeLikelihoodCalculator.Received(1).CalculateLikelihood(Arg.Any<ExpandedGenotype>());
        }
    }
}