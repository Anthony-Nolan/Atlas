using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
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
        private ISimulantsGenerator simulantsGenerator;
        
        [SetUp]
        public void SetUp()
        {
            poolGenerator = Substitute.For<INormalisedPoolGenerator>();
            testHarnessRepository = Substitute.For<ITestHarnessRepository>();
            simulantsGenerator = Substitute.For<ISimulantsGenerator>();

            testHarnessGenerator = new TestHarnessGenerator(poolGenerator, testHarnessRepository, simulantsGenerator);

            poolGenerator.GenerateNormalisedHaplotypeFrequencyPool().ReturnsForAnyArgs(
                new NormalisedHaplotypePool(default, new List<NormalisedPoolMember>()));
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
                new NormalisedHaplotypePool(poolId, new List<NormalisedPoolMember>()));

            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await testHarnessRepository.Received().AddTestHarness(poolId);
        }

        [Test]
        public async Task GenerateTestHarness_Simulates1000Patients()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await simulantsGenerator.Received().GenerateSimulants(Arg.Is<GenerateSimulantsRequest>(x =>
                x.SimulantCount == 1000 &&
                x.TestIndividualCategory == TestIndividualCategory.Patient));
        }

        [Test]
        public async Task GenerateTestHarness_Simulates10000Donors()
        {
            await testHarnessGenerator.GenerateTestHarness(new GenerateTestHarnessRequest());

            await simulantsGenerator.Received().GenerateSimulants(Arg.Is<GenerateSimulantsRequest>(x =>
                x.SimulantCount == 10000 &&
                x.TestIndividualCategory == TestIndividualCategory.Donor));
        }
    }
}
