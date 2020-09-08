using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;

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
            PhenotypeInfo<string> hlaTyping,
            int? sourceSimulantId = null)
        {
            return new Simulant
            {
                TestHarness_Id = request.TestHarnessId,
                TestIndividualCategory = request.TestIndividualCategory,
                SimulatedHlaTypingCategory = category,
                A_1 = hlaTyping.GetPosition(Locus.A, LocusPosition.One),
                A_2 = hlaTyping.GetPosition(Locus.A, LocusPosition.Two),
                B_1 = hlaTyping.GetPosition(Locus.B, LocusPosition.One),
                B_2 = hlaTyping.GetPosition(Locus.B, LocusPosition.Two),
                C_1 = hlaTyping.GetPosition(Locus.C, LocusPosition.One),
                C_2 = hlaTyping.GetPosition(Locus.C, LocusPosition.Two),
                DQB1_1 = hlaTyping.GetPosition(Locus.Dqb1, LocusPosition.One),
                DQB1_2 = hlaTyping.GetPosition(Locus.Dqb1, LocusPosition.Two),
                DRB1_1 = hlaTyping.GetPosition(Locus.Drb1, LocusPosition.One),
                DRB1_2 = hlaTyping.GetPosition(Locus.Drb1, LocusPosition.Two),
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
