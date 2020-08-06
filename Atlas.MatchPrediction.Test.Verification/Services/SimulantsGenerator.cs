using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    public interface ISimulantsGenerator
    {
        /// <summary>
        /// Generates and stores a set of simulated genotypes and test phenotypes.
        /// </summary>
        Task GenerateSimulants(GenerateSimulantsRequest request);
    }

    internal class SimulantsGenerator : ISimulantsGenerator
    {
        private readonly IGenotypeSimulator genotypeSimulator;
        private readonly ISimulantsRepository simulantsRepository;

        public SimulantsGenerator(IGenotypeSimulator genotypeSimulator, ISimulantsRepository simulantsRepository)
        {
            this.genotypeSimulator = genotypeSimulator;
            this.simulantsRepository = simulantsRepository;
        }
        
        public async Task GenerateSimulants(GenerateSimulantsRequest request)
        {
            var genotypes = await CreateGenotypeSimulants(request);

            // TODO ATLAS-478 - Mask genotypes
        }

        private async Task<IReadOnlyCollection<Simulant>> CreateGenotypeSimulants(GenerateSimulantsRequest request)
        {
            Debug.WriteLine($"Simulating {request.TestIndividualCategory} genotypes.");

            var genotypes = genotypeSimulator.SimulateGenotypes(request.SimulantCount, request.NormalisedHaplotypePool);
            
            var genotypeSimulants = genotypes
                .Select(g => MapToSimulantDatabaseModel(request, SimulatedHlaTypingCategory.Genotype, g))
                .ToList();

            await simulantsRepository.BulkInsertSimulants(genotypeSimulants);

            return genotypeSimulants;
        }

        private static Simulant MapToSimulantDatabaseModel(
            GenerateSimulantsRequest request,
            SimulatedHlaTypingCategory category,
            SimulatedHlaTyping hlaTyping,
            int? sourceSimulantId = null)
        {
            return new Simulant
            {
                TestHarness_Id = request.TestHarnessId,
                TestIndividualCategory = request.TestIndividualCategory,
                SimulatedHlaTypingCategory = category,
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
    }

    public class GenerateSimulantsRequest
    {
        public int TestHarnessId { get; set; }

        /// <summary>
        /// Data source for genotype generation.
        /// </summary>
        public NormalisedHaplotypePool NormalisedHaplotypePool { get; set; }

        public TestIndividualCategory TestIndividualCategory;

        /// <summary>
        /// Number of simulants of each type (genotype, masked) to create.
        /// </summary>
        public int SimulantCount;

        public LociInfo<MaskingCriterion> MaskingCriteria;
    }
}
