using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using FluentAssertions;
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
        private ILikelihoodCalculator likelihoodCalculator;

        [SetUp]
        public void SetUp()
        {
            genotypeImputer = Substitute.For<IGenotypeImputer>();
            frequencyRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
            setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
            likelihoodCalculator = Substitute.For<ILikelihoodCalculator>();

            setRepository.GetActiveSet(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new HaplotypeFrequencySet {Id = 1});

            genotypeImputer.GetPossibleDiplotypes(Arg.Any<PhenotypeInfo<string>>())
                .Returns(new List<Diplotype>
                {
                    new Diplotype
                    {
                        Item1 = new Haplotype {Hla = new LociInfo<string>()},
                        Item2 = new Haplotype {Hla = new LociInfo<string>()}
                    }
                });

            frequencyRepository.GetDiplotypeFrequencies(Arg.Any<List<LociInfo<string>>>(), Arg.Any<int>())
                .Returns(new Dictionary<LociInfo<string>, decimal> {{new LociInfo<string>(), 0}});

            likelihoodCalculator.CalculateLikelihood(Arg.Any<List<Diplotype>>())
                .Returns(0);

            genotypeLikelihoodService = new GenotypeLikelihoodService(
                setRepository,
                frequencyRepository,
                genotypeImputer,
                likelihoodCalculator
            );
        }

        [TestCase(16)]
        [TestCase(8)]
        [TestCase(4)]
        [TestCase(2)]
        [TestCase(1)]
        public async Task CalculateLikelihood_WithListOfHaplotypes_FrequencyRepositoryIsCalledOnce(int numberOfDiplotypes)
        {
            genotypeImputer.GetPossibleDiplotypes(Arg.Any<PhenotypeInfo<string>>())
                .Returns(Enumerable.Range(0, numberOfDiplotypes).Select(i => new Diplotype
                {
                    Item1 = new Haplotype {Hla = new LociInfo<string>()},
                    Item2 = new Haplotype {Hla = new LociInfo<string>()}
                }).ToList());

            await genotypeLikelihoodService.CalculateLikelihood(new GenotypeLikelihoodInput());

            await frequencyRepository.Received(1)
                .GetDiplotypeFrequencies(Arg.Any<IEnumerable<LociInfo<string>>>(), Arg.Any<int>());
        }

        [Test]
        public async Task CalculateLikelihood_WithListOfDiplotypes_LikelihoodCalculatorIsCalledOnce()
        {
            await genotypeLikelihoodService.CalculateLikelihood(new GenotypeLikelihoodInput());

            likelihoodCalculator.Received(1)
                .CalculateLikelihood(Arg.Any<List<Diplotype>>());
        }

        [TestCase(5)]
        [TestCase(0)]
        [TestCase(1.263452)]
        [TestCase(0.234534)]
        [TestCase(13.23453)]
        public async Task CalculateLikelihood_WhenLikelihoodIsCalculated_ReturnsResponseWithLikelihood(decimal expectedLikelihood)
        {
            likelihoodCalculator.CalculateLikelihood(Arg.Any<List<Diplotype>>())
                .Returns(expectedLikelihood);

            var actualLikelihoodResponse = await genotypeLikelihoodService.CalculateLikelihood(new GenotypeLikelihoodInput());

            actualLikelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }

    }
}