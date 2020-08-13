using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class TestHarnessGeneratorTests
    {
        private INormalisedPoolGenerator poolGenerator;
        private ITestHarnessRepository testHarnessRepository;
        private ITestHarnessGenerator testHarnessGenerator;
        private IGenotypeSimulantsGenerator genotypesGenerator;
        private IMaskedSimulantsGenerator maskedGenerator;

        [SetUp]
        public void SetUp()
        {
            poolGenerator = Substitute.For<INormalisedPoolGenerator>();
            testHarnessRepository = Substitute.For<ITestHarnessRepository>();
            genotypesGenerator = Substitute.For<IGenotypeSimulantsGenerator>();
            maskedGenerator = Substitute.For<IMaskedSimulantsGenerator>();

            testHarnessGenerator = new TestHarnessGenerator(
                poolGenerator, testHarnessRepository, genotypesGenerator, maskedGenerator);

            poolGenerator.GenerateNormalisedHaplotypeFrequencyPool().ReturnsForAnyArgs(
                new NormalisedHaplotypePool(default, default, new List<NormalisedPoolMember>()));
        }

        [Test]
        public async Task GenerateTestHarness_GeneratesNormalisedHaplotypeFrequencyPool()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await poolGenerator.Received().GenerateNormalisedHaplotypeFrequencyPool();
        }

        [Test]
        public async Task GenerateTestHarness_AddsTestHarnessWithPoolId()
        {
            const int poolId = 1;

            poolGenerator.GenerateNormalisedHaplotypeFrequencyPool().ReturnsForAnyArgs(
                new NormalisedHaplotypePool(poolId, default, new List<NormalisedPoolMember>()));

            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await testHarnessRepository.Received().AddTestHarness(poolId);
        }

        [Test]
        public async Task GenerateTestHarness_Simulates1000PatientGenotypes()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await genotypesGenerator.Received().GenerateSimulants(
                Arg.Is<GenerateSimulantsRequest>(x =>
                    x.SimulantCount == 1000 &&
                    x.TestIndividualCategory == TestIndividualCategory.Patient),
                Arg.Any<NormalisedHaplotypePool>());
        }

        [Test]
        public async Task GenerateTestHarness_Simulates1000PatientPhenotypes()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await maskedGenerator.Received().GenerateSimulants(
                Arg.Is<GenerateSimulantsRequest>(x =>
                    x.SimulantCount == 1000 &&
                    x.TestIndividualCategory == TestIndividualCategory.Patient),
                Arg.Any<MaskingRequests>(),
                Arg.Any<string>());
        }

        [Test]
        public async Task GenerateTestHarness_FirstSimulatesPatientGenotypesThenPhenotypes()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            Received.InOrder(async () =>
            {
                await genotypesGenerator.GenerateSimulants(
                    Arg.Is<GenerateSimulantsRequest>(x => x.TestIndividualCategory == TestIndividualCategory.Patient),
                    Arg.Any<NormalisedHaplotypePool>());
                await maskedGenerator.GenerateSimulants(
                    Arg.Is<GenerateSimulantsRequest>(x => x.TestIndividualCategory == TestIndividualCategory.Patient),
                    Arg.Any<MaskingRequests>(),
                    Arg.Any<string>());
            });
        }

        [Test]
        public async Task GenerateTestHarness_Simulates10000DonorGenotypes()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await genotypesGenerator.Received().GenerateSimulants(
                Arg.Is<GenerateSimulantsRequest>(x =>
                    x.SimulantCount == 10000 &&
                    x.TestIndividualCategory == TestIndividualCategory.Donor),
                Arg.Any<NormalisedHaplotypePool>());
        }

        [Test]
        public async Task GenerateTestHarness_Simulates10000DonorPhenotypes()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await maskedGenerator.Received().GenerateSimulants(
                Arg.Is<GenerateSimulantsRequest>(x =>
                    x.SimulantCount == 10000 &&
                    x.TestIndividualCategory == TestIndividualCategory.Donor),
                Arg.Any<MaskingRequests>(),
                Arg.Any<string>());
        }

        [Test]
        public async Task GenerateTestHarness_FirstSimulatesDonorGenotypesThenPhenotypes()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            Received.InOrder(async () =>
            {
                await genotypesGenerator.GenerateSimulants(
                    Arg.Is<GenerateSimulantsRequest>(x => x.TestIndividualCategory == TestIndividualCategory.Donor),
                    Arg.Any<NormalisedHaplotypePool>());
                await maskedGenerator.GenerateSimulants(
                    Arg.Is<GenerateSimulantsRequest>(x => x.TestIndividualCategory == TestIndividualCategory.Donor),
                    Arg.Any<MaskingRequests>(),
                    Arg.Any<string>());
            });
        }
    }
}
