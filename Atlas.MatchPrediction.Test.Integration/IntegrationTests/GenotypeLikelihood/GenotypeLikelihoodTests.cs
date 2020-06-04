using System.Collections.Generic;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.GenotypeLikelihood
{
    [TestFixture]
    public class GenotypeLikelihoodTests
    {
        private IGenotypeLikelihoodService service;

        [SetUp]
        public void SetUp()
        {
            service = DependencyInjection.DependencyInjection.Provider.GetService<IGenotypeLikelihoodService>();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(async () =>
            {
                var frequencyRepository = DependencyInjection.DependencyInjection.Provider
                    .GetService<IHaplotypeFrequenciesRepository>();
                var setRepository = DependencyInjection.DependencyInjection.Provider
                    .GetService<IHaplotypeFrequencySetRepository>();

                var setId = await AddHaplotypeFrequencySetsAvailableForLookup(setRepository);
                AddHaplotypeFrequenciesAvailableForLookup(frequencyRepository, setId);
            });
        }

        private static async Task<int> AddHaplotypeFrequencySetsAvailableForLookup(IHaplotypeFrequencySetRepository setRepository)
        {
            var frequencySet = new HaplotypeFrequencySet
            {
                RegistryCode = null,
                EthnicityCode = null,
                Active = true,
                Name = "Test"
            };

            var createdSet = await setRepository.AddSet(frequencySet);
            return createdSet.Id;
        }

        private static void AddHaplotypeFrequenciesAvailableForLookup(IHaplotypeFrequenciesRepository frequencyRepository, int setId)
        {
            var expectedDiplotypeHla = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency
                    {A = "A-2", B = "B-1", C = "C-1", DQB1 = "Dqb1-1", DRB1 = "Drb1-1", Frequency = (decimal) 0.32},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-2", C = "C-2", DQB1 = "Dqb1-2", DRB1 = "Drb1-2", Frequency = (decimal) 0.31},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-1", C = "C-1", DQB1 = "Dqb1-1", DRB1 = "Drb1-2", Frequency = (decimal) 0.301},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-2", C = "C-2", DQB1 = "Dqb1-2", DRB1 = "Drb1-1", Frequency = (decimal) 0.29},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-1", C = "C-1", DQB1 = "Dqb1-2", DRB1 = "Drb1-1", Frequency = (decimal) 0.28},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-2", C = "C-2", DQB1 = "Dqb1-1", DRB1 = "Drb1-2", Frequency = (decimal) 0.27},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-1", C = "C-1", DQB1 = "Dqb1-2", DRB1 = "Drb1-2", Frequency = (decimal) 0.26},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-2", C = "C-2", DQB1 = "Dqb1-1", DRB1 = "Drb1-1", Frequency = (decimal) 0.25},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-1", C = "C-2", DQB1 = "Dqb1-1", DRB1 = "Drb1-1", Frequency = (decimal) 0.24},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-2", C = "C-1", DQB1 = "Dqb1-2", DRB1 = "Drb1-2", Frequency = (decimal) 0.23},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-1", C = "C-2", DQB1 = "Dqb1-1", DRB1 = "Drb1-2", Frequency = (decimal) 0.22},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-2", C = "C-1", DQB1 = "Dqb1-2", DRB1 = "Drb1-1", Frequency = (decimal) 0.21},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-1", C = "C-2", DQB1 = "Dqb1-2", DRB1 = "Drb1-1", Frequency = (decimal) 0.201},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-2", C = "C-1", DQB1 = "Dqb1-1", DRB1 = "Drb1-2", Frequency = (decimal) 0.19},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-1", C = "C-2", DQB1 = "Dqb1-2", DRB1 = "Drb1-2", Frequency = (decimal) 0.18},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-2", C = "C-1", DQB1 = "Dqb1-1", DRB1 = "Drb1-1", Frequency = (decimal) 0.17},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-2", C = "C-1", DQB1 = "Dqb1-1", DRB1 = "Drb1-1", Frequency = (decimal) 0.16},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-1", C = "C-2", DQB1 = "Dqb1-2", DRB1 = "Drb1-2", Frequency = (decimal) 0.15},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-2", C = "C-1", DQB1 = "Dqb1-1", DRB1 = "Drb1-2", Frequency = (decimal) 0.14},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-1", C = "C-2", DQB1 = "Dqb1-2", DRB1 = "Drb1-1", Frequency = (decimal) 0.13},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-2", C = "C-1", DQB1 = "Dqb1-2", DRB1 = "Drb1-1", Frequency = (decimal) 0.12},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-1", C = "C-2", DQB1 = "Dqb1-1", DRB1 = "Drb1-2", Frequency = (decimal) 0.11},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-2", C = "C-1", DQB1 = "Dqb1-2", DRB1 = "Drb1-2", Frequency = (decimal) 0.101},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-1", C = "C-2", DQB1 = "Dqb1-1", DRB1 = "Drb1-1", Frequency = (decimal) 0.9},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-2", C = "C-2", DQB1 = "Dqb1-1", DRB1 = "Drb1-1", Frequency = (decimal) 0.8},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-1", C = "C-1", DQB1 = "Dqb1-2", DRB1 = "Drb1-2", Frequency = (decimal) 0.7},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-2", C = "C-2", DQB1 = "Dqb1-1", DRB1 = "Drb1-2", Frequency = (decimal) 0.6},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-1", C = "C-1", DQB1 = "Dqb1-2", DRB1 = "Drb1-1", Frequency = (decimal) 0.5},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-2", C = "C-2", DQB1 = "Dqb1-2", DRB1 = "Drb1-1", Frequency = (decimal) 0.4},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-1", C = "C-1", DQB1 = "Dqb1-1", DRB1 = "Drb1-2", Frequency = (decimal) 0.3},
                new HaplotypeFrequency
                    {A = "A-2", B = "B-2", C = "C-2", DQB1 = "Dqb1-2", DRB1 = "Drb1-2", Frequency = (decimal) 0.2},
                new HaplotypeFrequency
                    {A = "A-1", B = "B-1", C = "C-1", DQB1 = "Dqb1-1", DRB1 = "Drb1-1", Frequency = (decimal) 0.1}
            };

            frequencyRepository.AddHaplotypeFrequencies(setId, expectedDiplotypeHla);
        }

        [Test]
        public async Task CalculateLikelihood_WhenAllLociAreHeterozygous_ReturnsExpectedLikelihood()
        {
            var genotypeInput = new GenotypeLikelihoodInput {Genotype = PhenotypeInfoBuilder.New.Build()};
            const decimal expectedLikelihood = (decimal) 3.28716;

            var likelihoodResponse = await service.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }


        [TestCase(Locus.A, 1.4166)]
        [TestCase(Locus.B, 1.41646)]
        [TestCase(Locus.C, 0.92462)]
        [TestCase(Locus.Dqb1, 1.3765)]
        [TestCase(Locus.Drb1, 1.63234)]
        public async Task CalculateLikelihood_WhenLocusIsHomozygous_ReturnsExpectedLikelihood(Locus homozygousLocus, decimal expectedLikelihood)
        {
            var genotype = PhenotypeInfoBuilder.New.Build();
            genotype.SetPosition(homozygousLocus, LocusPosition.Two, genotype.GetPosition(homozygousLocus, LocusPosition.One));

            var genotypeInput = new GenotypeLikelihoodInput {Genotype = genotype};

            var likelihoodResponse = await service.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }

        [TestCase(new[] {Locus.A, Locus.C}, 0.6)]
        [TestCase(new[] {Locus.B, Locus.Drb1}, 0.8674)]
        [TestCase(new[] {Locus.C, Locus.Dqb1, Locus.B}, 0.2522)]
        [TestCase(new[] {Locus.Dqb1, Locus.A, Locus.Drb1, Locus.C}, 0.034)]
        public async Task CalculateLikelihood_WhenMultipleLocusAreHomozygous_ReturnsExpectedLikelihood(Locus[] homozygousLoci, decimal expectedLikelihood)
        {
            var genotype = PhenotypeInfoBuilder.New.Build();

            foreach (var homozygousLocus in homozygousLoci)
            {
                genotype.SetPosition(homozygousLocus, LocusPosition.Two, genotype.GetPosition(homozygousLocus, LocusPosition.One));
            }

            var genotypeInput = new GenotypeLikelihoodInput { Genotype = genotype };

            var likelihoodResponse = await service.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }

        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        public async Task CalculateLikelihood_WhenGenotypeHasNullLoci_Returns0Likelihood(Locus locusToBeNull)
        {
            var genotypeInput = new GenotypeLikelihoodInput {Genotype = PhenotypeInfoBuilder.New.Build()};
            genotypeInput.Genotype.SetLocus(locusToBeNull, null);

            const decimal expectedLikelihood = 0;

            var likelihoodResponse = await service.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }
    }
}