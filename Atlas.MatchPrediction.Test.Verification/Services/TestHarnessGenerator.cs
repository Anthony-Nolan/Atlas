using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    public interface ITestHarnessGenerator
    {
        Task<int> GenerateTestHarness();
    }

    public class TestHarnessGenerator : ITestHarnessGenerator
    {
        private readonly INormalisedPoolGenerator poolGenerator;
        private readonly IGenotypeSimulator genotypeSimulator;
        private readonly ITestHarnessRepository testHarnessRepository;
        private readonly ISimulantsRepository simulantsRepository;

        public TestHarnessGenerator(
            INormalisedPoolGenerator poolGenerator,
            IGenotypeSimulator genotypeSimulator,
            ITestHarnessRepository testHarnessRepository,
            ISimulantsRepository simulantsRepository)
        {
            this.poolGenerator = poolGenerator;
            this.genotypeSimulator = genotypeSimulator;
            this.testHarnessRepository = testHarnessRepository;
            this.simulantsRepository = simulantsRepository;
        }

        public async Task<int> GenerateTestHarness()
        {
            Debug.WriteLine("Generating normalised frequency pool.");
            var pool = await poolGenerator.GenerateNormalisedHaplotypeFrequencyPool();

            var testHarnessId = await testHarnessRepository.AddTestHarness(pool.Id);
            
            await CreatePatients(pool, testHarnessId);
            await CreateDonors(pool, testHarnessId);

            Debug.WriteLine("Done.");
            return testHarnessId;
        }

        private async Task CreatePatients(NormalisedHaplotypePool pool, int testHarnessId)
        {
            const int patientCount = 1000;

            var genotypes = await CreateGenotypeSimulants(pool, patientCount, new SimulantSetMetadata
            {
                TestHarnessId = testHarnessId,
                TestIndividualCategory = TestIndividualCategory.Patient,
                SimulatedHlaTypingCategory = SimulatedHlaTypingCategory.Genotype
            });

            // TODO ATLAS-478 - Mask genotypes
        }

        private async Task CreateDonors(NormalisedHaplotypePool pool, int testHarnessId)
        {
            const int donorCount = 10000;

            var genotypes = await CreateGenotypeSimulants(pool, donorCount, new SimulantSetMetadata
            {
                TestHarnessId = testHarnessId,
                TestIndividualCategory = TestIndividualCategory.Donor,
                SimulatedHlaTypingCategory = SimulatedHlaTypingCategory.Genotype
            });

            // TODO ATLAS-478 - Mask genotypes
        }

        private async Task<IReadOnlyCollection<Simulant>> CreateGenotypeSimulants(
            NormalisedHaplotypePool pool,
            int simulantCount,
            SimulantSetMetadata setMetadata)
        {
            Debug.WriteLine($"Simulating {setMetadata.TestIndividualCategory} genotypes.");

            var genotypes = genotypeSimulator.SimulateGenotypes(simulantCount, pool);
            var genotypeSimulants = genotypes.Select(g => MapToSimulantDatabaseModel(setMetadata, g)).ToList();

            await simulantsRepository.BulkInsertSimulants(genotypeSimulants);

            return genotypeSimulants;
        }

        private static Simulant MapToSimulantDatabaseModel(
            SimulantSetMetadata setMetadata,
            SimulatedHlaTyping hlaTyping,
            int? sourceSimulantId = null)
        {
            return new Simulant
            {
                TestHarness_Id = setMetadata.TestHarnessId,
                TestIndividualCategory = setMetadata.TestIndividualCategory,
                SimulatedHlaTypingCategory = setMetadata.SimulatedHlaTypingCategory,
                A_1 = hlaTyping.A_1,
                A_2 = hlaTyping.A_2,
                B_1 = hlaTyping.B_1,
                B_2 = hlaTyping.B_2,
                C_1 = hlaTyping.C_1,
                C_2 = hlaTyping.C_2,
                DQB1_1 = hlaTyping.Dqb1_1,
                DQB1_2 = hlaTyping.Dqb1_2,
                DRB1_1 = hlaTyping.Drb1_1,
                DRB1_2 = hlaTyping.Drb1_2,
                SourceSimulantId = sourceSimulantId
            };
        }

        private class SimulantSetMetadata
        {
            public int TestHarnessId { get; set; }
            public TestIndividualCategory TestIndividualCategory { get; set; }
            public SimulatedHlaTypingCategory SimulatedHlaTypingCategory { get; set; }
        }
    }
}
