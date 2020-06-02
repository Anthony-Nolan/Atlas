using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    public class GenotypeLikelihoodServiceTests
    {
        private IGenotypeLikelihoodService genotypeLikelihoodService;
        private IGenotypeImputer genotypeImputer;
        private IHaplotypeFrequenciesRepository haplotypeFrequenciesRepository;

        [SetUp]
        public void SetUp()
        {
            genotypeImputer = Substitute.For<IGenotypeImputer>();
            haplotypeFrequenciesRepository = Substitute.For<IHaplotypeFrequenciesRepository>();

            genotypeImputer.GetPossibleDiplotypes(Arg.Any<PhenotypeInfo<string>>())
                .Returns(new List<Diplotype>());

            haplotypeFrequenciesRepository.GetDiplotypeFrequencies(Arg.Any<List<LociInfo<string>>>())
                .Returns(new Dictionary<LociInfo<string>, decimal>());

            genotypeLikelihoodService = new GenotypeLikelihoodService(
                haplotypeFrequenciesRepository,
                genotypeImputer
            );
        }

        [TestCase(16)]
        [TestCase(8)]
        [TestCase(4)]
        [TestCase(2)]
        [TestCase(1)]
        public async Task GetDiplotypeFrequencies_WithAnyAmountOfHaplotypes_IsCalledOnce(int numberOfDiplotypes)
        {
            var diplotypes = new List<Diplotype>();

            for (var i = 0; i < numberOfDiplotypes; i++)
            {
                diplotypes.Add( new Diplotype(PhenotypeInfoBuilder.New.Build()));
            }

            genotypeImputer
                .GetPossibleDiplotypes(Arg.Any<PhenotypeInfo<string>>())
                .Returns(diplotypes);

            await genotypeLikelihoodService.CalculateLikelihood(new GenotypeLikelihoodInput());

            await haplotypeFrequenciesRepository.Received(1)
                .GetDiplotypeFrequencies(Arg.Any<IEnumerable<LociInfo<string>>>());
        }
    }
}