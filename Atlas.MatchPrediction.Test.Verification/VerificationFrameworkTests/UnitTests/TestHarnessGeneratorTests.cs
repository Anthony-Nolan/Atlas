using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class TestHarnessGeneratorTests
    {
        private INormalisedPoolGenerator poolGenerator;
        private IGenotypeSimulator genotypeSimulator;
        private ITestHarnessRepository testHarnessRepository;
        private ISimulantsRepository simulantsRepository;
        private ITestHarnessGenerator testHarnessGenerator;

        [SetUp]
        public void SetUp()
        {
            poolGenerator = Substitute.For<INormalisedPoolGenerator>();
            genotypeSimulator = Substitute.For<IGenotypeSimulator>();
            testHarnessRepository = Substitute.For<ITestHarnessRepository>();
            simulantsRepository = Substitute.For<ISimulantsRepository>();

            testHarnessGenerator = new TestHarnessGenerator(
                poolGenerator, genotypeSimulator, testHarnessRepository, simulantsRepository);

            poolGenerator.GenerateNormalisedHaplotypeFrequencyPool().ReturnsForAnyArgs(
                new NormalisedHaplotypePool(default, new List<NormalisedPoolMember>()));
        }

        [Test]
        public async Task GenerateTestHarness_GeneratesNormalisedHaplotypeFrequencyPool()
        {
            await testHarnessGenerator.GenerateTestHarness();

            await poolGenerator.Received().GenerateNormalisedHaplotypeFrequencyPool();
        }

        [Test]
        public async Task GenerateTestHarness_AddsTestHarnessWithPoolId()
        {
            const int poolId = 1;

            poolGenerator.GenerateNormalisedHaplotypeFrequencyPool().ReturnsForAnyArgs(
                new NormalisedHaplotypePool(poolId, new List<NormalisedPoolMember>()));

            await testHarnessGenerator.GenerateTestHarness();

            await testHarnessRepository.Received().AddTestHarness(poolId);
        }

        [Test]
        public async Task GenerateTestHarness_Simulates1000Genotypes()
        {
            await testHarnessGenerator.GenerateTestHarness();

            // cannot determine that these are *patient* genotypes without integration testing
            genotypeSimulator.Received().SimulateGenotypes(1000, Arg.Any<NormalisedHaplotypePool>());
        }

        [Test]
        public async Task GenerateTestHarness_Simulates10000Genotypes()
        {
            await testHarnessGenerator.GenerateTestHarness();

            // cannot determine that these are *donor* genotypes without integration testing
            genotypeSimulator.Received().SimulateGenotypes(10000, Arg.Any<NormalisedHaplotypePool>());
        }

        [Test]
        public async Task GenerateTestHarness_WritesPatientGenotypesToDatabaseWithCorrectMetadata()
        {
            const int testHarnessId = 123;
            testHarnessRepository.AddTestHarness(default).ReturnsForAnyArgs(testHarnessId);

            // use genotype count to differentiate between calls to create patients and donors
            const int genotypeCount = 1000;
            genotypeSimulator.SimulateGenotypes(genotypeCount, default).ReturnsForAnyArgs(new[] { new SimulatedHlaTyping() });

            await testHarnessGenerator.GenerateTestHarness();

            await simulantsRepository.Received().BulkInsertSimulants(Arg.Is<IReadOnlyCollection<Simulant>>(x =>
                x.First().TestHarness_Id == testHarnessId &&
                x.First().TestIndividualCategory == TestIndividualCategory.Patient &&
                x.First().SimulatedHlaTypingCategory == SimulatedHlaTypingCategory.Genotype &&
                x.First().SourceSimulantId == null
                ));
        }

        [Test]
        public async Task GenerateTestHarness_WritesDonorGenotypesToDatabaseWithCorrectMetadata()
        {
            const int testHarnessId = 123;
            testHarnessRepository.AddTestHarness(default).ReturnsForAnyArgs(testHarnessId);

            // use genotype count to differentiate between calls to create patients and donors
            const int genotypeCount = 10000;
            genotypeSimulator.SimulateGenotypes(genotypeCount, default).ReturnsForAnyArgs(new[] { new SimulatedHlaTyping() });

            await testHarnessGenerator.GenerateTestHarness();

            await simulantsRepository.Received().BulkInsertSimulants(Arg.Is<IReadOnlyCollection<Simulant>>(x =>
                x.First().TestHarness_Id == testHarnessId &&
                x.First().TestIndividualCategory == TestIndividualCategory.Donor &&
                x.First().SimulatedHlaTypingCategory == SimulatedHlaTypingCategory.Genotype &&
                x.First().SourceSimulantId == null
            ));
        }

        [Test]
        public async Task GenerateTestHarness_WritesGenotypesToDatabaseWithCorrectlyMappedHla()
        {
            genotypeSimulator.SimulateGenotypes(default, default).ReturnsForAnyArgs(new[]
            {
                new SimulatedHlaTyping
                {
                    A_1 = "a-1",
                    A_2 = "a-2",
                    B_1 = "b-1",
                    B_2 = "b-2",
                    C_1 = "c-1",
                    C_2 = "c-2",
                    Dqb1_1 = "dqb1-1",
                    Dqb1_2 = "dqb1-2",
                    Drb1_1 = "drb1-1",
                    Drb1_2 = "drb1-2"
                }
            });

            await testHarnessGenerator.GenerateTestHarness();

            await simulantsRepository.Received().BulkInsertSimulants(Arg.Is<IReadOnlyCollection<Simulant>>(x =>
                x.First().A_1.Equals("a-1") &&
                x.First().A_2.Equals("a-2") &&
                x.First().B_1.Equals("b-1") &&
                x.First().B_2.Equals("b-2") &&
                x.First().C_1.Equals("c-1") &&
                x.First().C_2.Equals("c-2") &&
                x.First().DQB1_1.Equals("dqb1-1") &&
                x.First().DQB1_2.Equals("dqb1-2") &&
                x.First().DRB1_1.Equals("drb1-1") &&
                x.First().DRB1_2.Equals("drb1-2")));
        }

        // TODO ATLAS-478 - Mask genotypes tests
    }
}
