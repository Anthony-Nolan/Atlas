using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class MaskedSimulantsGeneratorTests
    {
        private ILocusHlaMasker locusHlaMasker;
        private ISimulantsRepository simulantsRepository;
        private ITestHarnessRepository testHarnessRepository;

        private IMaskedSimulantsGenerator simulantsGenerator;

        [SetUp]
        public void SetUp()
        {
            locusHlaMasker = Substitute.For<ILocusHlaMasker>();
            simulantsRepository = Substitute.For<ISimulantsRepository>();
            testHarnessRepository = Substitute.For<ITestHarnessRepository>();

            simulantsGenerator = new MaskedSimulantsGenerator(locusHlaMasker, testHarnessRepository, simulantsRepository);

            simulantsRepository.GetGenotypeSimulants(default, default)
                .ReturnsForAnyArgs(new List<Simulant>());

        }

        [TestCase(TestIndividualCategory.Donor)]
        [TestCase(TestIndividualCategory.Patient)]
        public async Task GenerateSimulants_GetsCorrectGenotypesFromDatabase(TestIndividualCategory category)
        {
            const int testHarnessId = 1;

            await simulantsGenerator.GenerateSimulants(
                new GenerateSimulantsRequest
                {
                    TestHarnessId = testHarnessId,
                    TestIndividualCategory = category
                },
                new MaskingRequests(),
                default);

            await simulantsRepository.Received().GetGenotypeSimulants(testHarnessId, category.ToString());
        }

        [TestCase(0, 100)]
        [TestCase(100, 10)]
        public void GenerateSimulants_NumberOfGenotypesDoesNotMatchSimulantCount_ThrowsException(int genotypeCount, int simulantCount)
        {
            simulantsRepository.GetGenotypeSimulants(default, default)
                .ReturnsForAnyArgs(SimulantBuilder.Default.Build(genotypeCount));

            simulantsGenerator
                .Invoking(async x => await x.GenerateSimulants(
                    new GenerateSimulantsRequest { SimulantCount = simulantCount },
                    new MaskingRequests(),
                    default))
                .Should().Throw<Exception>();
        }

        [Test]
        public async Task GenerateSimulants_MasksGenotypesByMatchPredictionLocus()
        {
            const int genotypeCount = 1;
            simulantsRepository.GetGenotypeSimulants(default, default)
                .ReturnsForAnyArgs(SimulantBuilder.Default.Build(genotypeCount));

            await simulantsGenerator.GenerateSimulants(
                new GenerateSimulantsRequest { SimulantCount = genotypeCount },
                new MaskingRequests(),
                default);

            await locusHlaMasker.Received(1).MaskHlaForSingleLocus(
                Arg.Any<LocusMaskingRequests>(),
                Arg.Is<IReadOnlyCollection<SimulantLocusHla>>(x => x.Single().Locus == Locus.A));
            await locusHlaMasker.Received(1).MaskHlaForSingleLocus(
                Arg.Any<LocusMaskingRequests>(),
                Arg.Is<IReadOnlyCollection<SimulantLocusHla>>(x => x.Single().Locus == Locus.B));
            await locusHlaMasker.Received(1).MaskHlaForSingleLocus(
                Arg.Any<LocusMaskingRequests>(),
                Arg.Is<IReadOnlyCollection<SimulantLocusHla>>(x => x.Single().Locus == Locus.C));
            await locusHlaMasker.Received(1).MaskHlaForSingleLocus(
                Arg.Any<LocusMaskingRequests>(),
                Arg.Is<IReadOnlyCollection<SimulantLocusHla>>(x => x.Single().Locus == Locus.Dqb1));
            await locusHlaMasker.Received(1).MaskHlaForSingleLocus(
                Arg.Any<LocusMaskingRequests>(),
                Arg.Is<IReadOnlyCollection<SimulantLocusHla>>(x => x.Single().Locus == Locus.Drb1));

            await locusHlaMasker.DidNotReceive().MaskHlaForSingleLocus(
                Arg.Any<LocusMaskingRequests>(),
                Arg.Is<IReadOnlyCollection<SimulantLocusHla>>(x => x.Single().Locus == Locus.Dpb1));
        }

        [Test]
        public async Task GenerateSimulants_MasksAllGenotypes()
        {
            const string firstHla = "hla-1";
            const string secondHla = "hla-2";

            var firstGenotype = SimulantBuilder.New.WithHlaAtEveryLocus(firstHla).Build();
            var secondGenotype = SimulantBuilder.New.WithHlaAtEveryLocus(secondHla).Build();
            var genotypes = new List<Simulant> { firstGenotype, secondGenotype };

            simulantsRepository.GetGenotypeSimulants(default, default)
                .ReturnsForAnyArgs(genotypes);

            await simulantsGenerator.GenerateSimulants(
                new GenerateSimulantsRequest { SimulantCount = genotypes.Count },
                new MaskingRequests(),
                default);

            await locusHlaMasker.Received(5).MaskHlaForSingleLocus(
                Arg.Any<LocusMaskingRequests>(),
                Arg.Is<IReadOnlyCollection<SimulantLocusHla>>(x =>
                    x.Select(g => g.GenotypeSimulantId).SequenceEqual(new[] { firstGenotype.Id, secondGenotype.Id }) &&
                    x.Select(g => g.HlaTyping).SequenceEqual(new[] { new LocusInfo<string>(firstHla), new LocusInfo<string>(secondHla) })));
        }

        [Test]
        public async Task GenerateSimulants_BuildsSimulantFromMaskedLociTypings()
        {
            locusHlaMasker.MaskHlaForSingleLocus(default, default).ReturnsForAnyArgs(
                BuildHlaFor(Locus.A),
BuildHlaFor(Locus.B),
                BuildHlaFor(Locus.C),
                BuildHlaFor(Locus.Dqb1),
                BuildHlaFor(Locus.Drb1)
                );

            await simulantsGenerator.GenerateSimulants(new GenerateSimulantsRequest(), new MaskingRequests(), default);

            await simulantsRepository.Received().BulkInsertSimulants(Arg.Is<IReadOnlyCollection<Simulant>>(x =>
                x.First().A_1.Equals("A-1") &&
                x.First().A_2.Equals("A-2") &&
                x.First().B_1.Equals("B-1") &&
                x.First().B_2.Equals("B-2") &&
                x.First().C_1.Equals("C-1") &&
                x.First().C_2.Equals("C-2") &&
                x.First().DQB1_1.Equals("Dqb1-1") &&
                x.First().DQB1_2.Equals("Dqb1-2") &&
                x.First().DRB1_1.Equals("Drb1-1") &&
                x.First().DRB1_2.Equals("Drb1-2")));
        }

        [TestCase(TestIndividualCategory.Donor)]
        [TestCase(TestIndividualCategory.Patient)]
        public async Task GenerateSimulants_WritesPhenotypesToDatabaseWithCorrectMetadata(TestIndividualCategory testIndividualCategory)
        {
            const int testHarnessId = 123;

            var typings = locusHlaMasker.MaskHlaForSingleLocus(default, default).ReturnsForAnyArgs(
                BuildHlaFor(Locus.A),
                BuildHlaFor(Locus.B),
                BuildHlaFor(Locus.C),
                BuildHlaFor(Locus.Dqb1),
                BuildHlaFor(Locus.Drb1)
            );

            await simulantsGenerator.GenerateSimulants(
                new GenerateSimulantsRequest
                {
                    TestHarnessId = testHarnessId,
                    TestIndividualCategory = testIndividualCategory
                },
                new MaskingRequests(),
                default);

            await simulantsRepository.Received().BulkInsertSimulants(Arg.Is<IReadOnlyCollection<Simulant>>(x =>
                x.First().TestHarness_Id == testHarnessId &&
                x.First().TestIndividualCategory == testIndividualCategory &&
                x.First().SimulatedHlaTypingCategory == SimulatedHlaTypingCategory.Masked &&
                x.First().SourceSimulantId != null
                ));
        }

        private static IReadOnlyCollection<SimulantLocusHla> BuildHlaFor(Locus locus)
        {
            return SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(1).ToList();
        }
    }
}
