using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    public interface ITestHarnessGenerator
    {
        Task<int> GenerateTestHarness(GenerateTestHarnessRequest request);
    }

    public class TestHarnessGenerator : ITestHarnessGenerator
    {
        private readonly INormalisedPoolGenerator poolGenerator;
        private readonly ITestHarnessRepository testHarnessRepository;
        private readonly ISimulantsGenerator simulantsGenerator;

        public TestHarnessGenerator(
            INormalisedPoolGenerator poolGenerator,
            ITestHarnessRepository testHarnessRepository,
            ISimulantsGenerator simulantsGenerator)
        {
            this.poolGenerator = poolGenerator;
            this.testHarnessRepository = testHarnessRepository;
            this.simulantsGenerator = simulantsGenerator;
        }

        public async Task<int> GenerateTestHarness(GenerateTestHarnessRequest request)
        {
            Debug.WriteLine("Generating normalised frequency pool.");
            var pool = await poolGenerator.GenerateNormalisedHaplotypeFrequencyPool();

            var testHarnessId = await testHarnessRepository.AddTestHarness(pool.Id);
            
            await CreatePatients(pool, testHarnessId, request.PatientMaskingCriteria.ToLociInfo());
            await CreateDonors(pool, testHarnessId, request.DonorMaskingCriteria.ToLociInfo());

            Debug.WriteLine("Done.");
            return testHarnessId;
        }

        private async Task CreatePatients(NormalisedHaplotypePool pool, int testHarnessId, LociInfo<MaskingCriterion> maskingCriteria)
        {
            const int patientCount = 1000;

            await simulantsGenerator.GenerateSimulants(new GenerateSimulantsRequest
            {
                TestHarnessId = testHarnessId,
                NormalisedHaplotypePool = pool,
                TestIndividualCategory = TestIndividualCategory.Patient,
                SimulantCount = patientCount,
                MaskingCriteria = maskingCriteria
            });
        }

        private async Task CreateDonors(NormalisedHaplotypePool pool, int testHarnessId, LociInfo<MaskingCriterion> maskingCriteria)
        {
            const int donorCount = 10000;

            await simulantsGenerator.GenerateSimulants(new GenerateSimulantsRequest
            {
                TestHarnessId = testHarnessId,
                NormalisedHaplotypePool = pool,
                TestIndividualCategory = TestIndividualCategory.Donor,
                SimulantCount = donorCount,
                MaskingCriteria = maskingCriteria
            });
        }
    }
}
