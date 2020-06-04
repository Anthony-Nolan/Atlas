using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.GenotypeLikelihood;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.GenotypeLikelihood
{
    ///<summary>
    /// These tests are snapshots based on some manually calculated frequencies/expectations.
    /// Any tests of the GenotypeLikelihood calculator that are not such tests should be added elsewhere.
    ///</summary>
    [TestFixture]
    public class GenotypeLikelihoodTests
    {
        private IFrequencySetService importService;
        private IGenotypeLikelihoodService likelihoodService;

        private const string A1 = "A-1";
        private const string A2 = "A-2";
        private const string B1 = "B-1";
        private const string B2 = "B-2";
        private const string C1 = "C-1";
        private const string C2 = "C-2";
        private const string Dqb11 = "Dqb1-1";
        private const string Dqb12 = "Dqb1-2";
        private const string Drb11 = "Drb1-1";
        private const string Drb12 = "Drb1-2";

        [SetUp]
        public async Task SetUp()
        {
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
            likelihoodService = DependencyInjection.DependencyInjection.Provider.GetService<IGenotypeLikelihoodService>();

            // 32 possible haplotypes for a single unambiguous genotype.
            var allPossibleHaplotypes = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C1, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.32},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C2, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.31},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C1, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.301},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C2, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.29},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C1, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.28},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C2, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.27},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C1, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.26},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C2, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.25},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C2, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.24},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C1, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.23},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C2, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.22},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C1, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.21},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C2, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.201},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C1, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.19},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C2, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.18},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C1, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.17},
                new HaplotypeFrequency
                    {A = A2, B = B2, C = C1, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.16},
                new HaplotypeFrequency
                    {A = A1, B = B1, C = C2, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.15},
                new HaplotypeFrequency
                    {A = A2, B = B2, C = C1, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.14},
                new HaplotypeFrequency
                    {A = A1, B = B1, C = C2, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.13},
                new HaplotypeFrequency
                    {A = A2, B = B2, C = C1, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.12},
                new HaplotypeFrequency
                    {A = A1, B = B1, C = C2, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.11},
                new HaplotypeFrequency
                    {A = A2, B = B2, C = C1, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.101},
                new HaplotypeFrequency
                    {A = A1, B = B1, C = C2, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.9},
                new HaplotypeFrequency
                    {A = A2, B = B2, C = C2, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.8},
                new HaplotypeFrequency
                    {A = A1, B = B1, C = C1, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.7},
                new HaplotypeFrequency
                    {A = A2, B = B2, C = C2, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.6},
                new HaplotypeFrequency
                    {A = A1, B = B1, C = C1, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.5},
                new HaplotypeFrequency
                    {A = A2, B = B2, C = C2, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.4},
                new HaplotypeFrequency
                    {A = A1, B = B1, C = C1, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.3},
                new HaplotypeFrequency
                    {A = A2, B = B2, C = C2, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.2},
                new HaplotypeFrequency
                    {A = A1, B = B1, C = C1, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.1}
            };

            await ImportFrequencies(allPossibleHaplotypes);
        }

        [Test]
        public async Task CalculateLikelihood_WhenAllLociAreHeterozygous_ReturnsExpectedLikelihood()
        {
            var genotypeInput = new GenotypeLikelihoodInput {Genotype = PhenotypeInfoBuilder.New.Build()};
            const decimal expectedLikelihood = (decimal) 3.28716;

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }

        [TestCase(Locus.A, 1.4166)]
        [TestCase(Locus.B, 1.41646)]
        [TestCase(Locus.C, 0.92462)]
        [TestCase(Locus.Dqb1, 1.3765)]
        [TestCase(Locus.Drb1, 1.63234)]
        public async Task CalculateLikelihood_WhenLocusIsHomozygous_ReturnsExpectedLikelihood(Locus homozygousLocus, decimal expectedLikelihood)
        {
            var genotype = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
                .With(d => d.B, new LocusInfo<string> {Position1 = B1, Position2 = B2})
                .With(d => d.C, new LocusInfo<string> {Position1 = C1, Position2 = C2})
                .With(d => d.Dqb1, new LocusInfo<string> {Position1 = Dqb11, Position2 = Dqb12})
                .With(d => d.Drb1, new LocusInfo<string> {Position1 = Drb11, Position2 = Drb12})
                .Build();

            genotype.SetPosition(homozygousLocus, LocusPosition.Two,
                genotype.GetPosition(homozygousLocus, LocusPosition.One));

            var genotypeInput = new GenotypeLikelihoodInput {Genotype = genotype};

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }

        [TestCase(new[] {Locus.A, Locus.C}, 0.6)]
        [TestCase(new[] {Locus.B, Locus.Drb1}, 0.8674)]
        [TestCase(new[] {Locus.C, Locus.Dqb1, Locus.B}, 0.2522)]
        [TestCase(new[] {Locus.Dqb1, Locus.A, Locus.Drb1, Locus.C}, 0.034)]
        public async Task CalculateLikelihood_WhenMultipleLocusAreHomozygous_ReturnsExpectedLikelihood(Locus[] homozygousLoci, decimal expectedLikelihood)
        {
            var genotype = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
                .With(d => d.B, new LocusInfo<string> {Position1 = B1, Position2 = B2})
                .With(d => d.C, new LocusInfo<string> {Position1 = C1, Position2 = C2})
                .With(d => d.Dqb1, new LocusInfo<string> {Position1 = Dqb11, Position2 = Dqb12})
                .With(d => d.Drb1, new LocusInfo<string> {Position1 = Drb11, Position2 = Drb12})
                .Build();

            foreach (var homozygousLocus in homozygousLoci)
            {
                genotype.SetPosition(homozygousLocus, LocusPosition.Two,
                    genotype.GetPosition(homozygousLocus, LocusPosition.One));
            }

            var genotypeInput = new GenotypeLikelihoodInput {Genotype = genotype};

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }

        //TODO: ATLAS-345: This test will no longer be correct once we handle untyped input loci.
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        public async Task CalculateLikelihood_WhenGenotypeHasNullLoci_Returns0Likelihood(Locus locusToBeNull)
        {
            var genotype = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
                .With(d => d.B, new LocusInfo<string> {Position1 = B1, Position2 = B2})
                .With(d => d.C, new LocusInfo<string> {Position1 = C1, Position2 = C2})
                .With(d => d.Dqb1, new LocusInfo<string> {Position1 = Dqb11, Position2 = Dqb12})
                .With(d => d.Drb1, new LocusInfo<string> {Position1 = Drb11, Position2 = Drb12})
                .Build();

            var genotypeInput = new GenotypeLikelihoodInput {Genotype = genotype};
            genotypeInput.Genotype.SetLocus(locusToBeNull, null);

            const decimal expectedLikelihood = 0;

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public async Task CalculateLikelihood_WhenNoHaplotypesAreRepresentedInDatabase_Returns0Likelihood(Locus unrepresentedLocus)
        {
            var genotype = PhenotypeInfoBuilder.New
                .With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
                .With(d => d.B, new LocusInfo<string> {Position1 = B1, Position2 = B2})
                .With(d => d.C, new LocusInfo<string> {Position1 = C1, Position2 = C2})
                .With(d => d.Dqb1, new LocusInfo<string> {Position1 = Dqb11, Position2 = Dqb12})
                .With(d => d.Drb1, new LocusInfo<string> {Position1 = Drb11, Position2 = Drb12})
                .Build();

            var genotypeInput = new GenotypeLikelihoodInput {Genotype = genotype};
            genotypeInput.Genotype.SetLocus(unrepresentedLocus, "un-represented");

            const decimal expectedLikelihood = 0;

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }


        [Test]
        public async Task CalculateLikelihood_WhenOnlySomeHaplotypesAreRepresentedInDatabase_ReturnsExpectedLikelihood()
        {
            // 16 of the possible 32 haplotypes for a single unambiguous genotype.
            var haplotypesWith16Missing = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C1, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.32},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C2, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.31},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C1, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.301},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C2, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.29},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C1, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.28},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C2, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.27},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C1, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.26},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C2, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.25},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C2, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.24},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C1, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.23},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C2, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.22},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C1, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.21},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C2, DQB1 = Dqb12, DRB1 = Drb11, Frequency = (decimal) 0.201},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C1, DQB1 = Dqb11, DRB1 = Drb12, Frequency = (decimal) 0.19},
                new HaplotypeFrequency
                    {A = A2, B = B1, C = C2, DQB1 = Dqb12, DRB1 = Drb12, Frequency = (decimal) 0.18},
                new HaplotypeFrequency
                    {A = A1, B = B2, C = C1, DQB1 = Dqb11, DRB1 = Drb11, Frequency = (decimal) 0.17}
            };

            await ImportFrequencies(haplotypesWith16Missing);

            var genotypeInput = new GenotypeLikelihoodInput { Genotype = PhenotypeInfoBuilder.New.Build() };
            const decimal expectedLikelihood = (decimal)0.99456;

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Likelihood.Should().Be(expectedLikelihood);
        }

        private async Task ImportFrequencies(IEnumerable<HaplotypeFrequency> haplotypes)
        {
            var file = FrequenciesFileBuilder.Build(haplotypes);

            await using (var stream = GetHaplotypeFrequenciesStream(file.Contents))
            {
                await importService.ImportFrequencySet(stream, file.FileName);
            }
        }

        private static Stream GetHaplotypeFrequenciesStream(string fileContents)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(fileContents));
        }
    }
}