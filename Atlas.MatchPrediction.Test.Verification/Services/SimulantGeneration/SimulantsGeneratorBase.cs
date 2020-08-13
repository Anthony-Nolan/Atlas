using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration
{
    internal abstract class SimulantsGeneratorBase
    {
        private readonly ISimulantsRepository simulantsRepository;

        protected SimulantsGeneratorBase(ISimulantsRepository simulantsRepository)
        {
            this.simulantsRepository = simulantsRepository;
        }

        protected static Simulant MapToSimulantDatabaseModel(
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
        
        protected async Task StoreSimulants(IReadOnlyCollection<Simulant> simulants)
        {
            await simulantsRepository.BulkInsertSimulants(simulants);
        }

        protected async Task<IReadOnlyCollection<Simulant>> ReadGenotypeSimulants(int testHarnessId, TestIndividualCategory category)
        {
            return (await simulantsRepository.GetGenotypeSimulants(testHarnessId, category.ToString())).ToList();
        }
    }
}
