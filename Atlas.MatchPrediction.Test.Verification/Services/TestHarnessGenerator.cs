using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    public interface ITestHarnessGenerator
    {
        Task<int> GenerateTestHarness(GenerateTestHarnessRequest request);
    }

    internal class TestHarnessGenerator : ITestHarnessGenerator
    {
        private readonly INormalisedPoolGenerator poolGenerator;
        private readonly ITestHarnessRepository testHarnessRepository;
        private readonly IGenotypeSimulantsGenerator genotypesGenerator;
        private readonly IMaskedSimulantsGenerator maskedGenerator;

        public TestHarnessGenerator(
            INormalisedPoolGenerator poolGenerator,
            ITestHarnessRepository testHarnessRepository,
            IGenotypeSimulantsGenerator genotypesGenerator,
            IMaskedSimulantsGenerator maskedGenerator)
        {
            this.poolGenerator = poolGenerator;
            this.testHarnessRepository = testHarnessRepository;
            this.genotypesGenerator = genotypesGenerator;
            this.maskedGenerator = maskedGenerator;
        }

        public async Task<int> GenerateTestHarness(GenerateTestHarnessRequest request)
        {
            System.Diagnostics.Debug.WriteLine("Generating normalised frequency pool.");
            var pool = await poolGenerator.GenerateNormalisedHaplotypeFrequencyPool();

            var testHarnessId = await testHarnessRepository.AddTestHarness(pool.Id, request.Comments);
            
            await CreatePatients(pool, testHarnessId, request.PatientMaskingRequests.ToMaskingRequests());
            await CreateDonors(pool, testHarnessId, request.DonorMaskingRequests.ToMaskingRequests());

            await testHarnessRepository.MarkAsCompleted(testHarnessId);

            System.Diagnostics.Debug.WriteLine("Done.");
            return testHarnessId;
        }

        private async Task CreatePatients(NormalisedHaplotypePool pool, int testHarnessId, MaskingRequests maskingRequests)
        {
            const int patientCount = 1000;

            var request = new GenerateSimulantsRequest
            {
                TestHarnessId = testHarnessId,
                TestIndividualCategory = TestIndividualCategory.Patient,
                SimulantCount = patientCount
            };

            await GenerateSimulants(request, pool, maskingRequests);
        }

        private async Task CreateDonors(NormalisedHaplotypePool pool, int testHarnessId, MaskingRequests maskingRequests)
        {
            const int donorCount = 10000;

            var request = new GenerateSimulantsRequest
            {
                TestHarnessId = testHarnessId,
                TestIndividualCategory = TestIndividualCategory.Donor,
                SimulantCount = donorCount
            };

            await GenerateSimulants(request, pool, maskingRequests);
        }

        private async Task GenerateSimulants(
            GenerateSimulantsRequest request,
            NormalisedHaplotypePool pool,
            MaskingRequests maskingRequests)
        {
            await genotypesGenerator.GenerateSimulants(request, pool);
            await maskedGenerator.GenerateSimulants(request, maskingRequests, pool.HlaNomenclatureVersion, pool.TypingCategory);
        }
    }
}
