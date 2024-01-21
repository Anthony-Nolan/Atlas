using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration
{
    internal interface IGenotypeSimulantsGenerator
    {
        /// <summary>
        /// Generates and stores a set of simulated genotypes.
        /// </summary>
        Task GenerateSimulants(GenerateSimulantsRequest request, NormalisedHaplotypePool pool);
    }

    internal class GenotypeSimulantsGenerator : SimulantsGeneratorBase, IGenotypeSimulantsGenerator
    {
        private readonly IGenotypeSimulator genotypeSimulator;

        public GenotypeSimulantsGenerator(IGenotypeSimulator genotypeSimulator, ISimulantsRepository simulantsRepository) 
            : base(simulantsRepository)
        {
            this.genotypeSimulator = genotypeSimulator;
        }

        public async Task GenerateSimulants(GenerateSimulantsRequest request, NormalisedHaplotypePool pool)
        {
            System.Diagnostics.Debug.WriteLine($"Simulating {request.TestIndividualCategory} genotypes.");

            var genotypes = genotypeSimulator.SimulateGenotypes(request.SimulantCount, pool);

            var genotypeSimulants = genotypes
                .Select(g => MapToSimulantDatabaseModel(request, SimulatedHlaTypingCategory.Genotype, g))
                .ToList();

            await StoreSimulants(genotypeSimulants);
        }
    }
}
