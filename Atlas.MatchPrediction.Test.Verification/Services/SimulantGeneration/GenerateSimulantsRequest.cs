using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;

namespace Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration
{
    internal class GenerateSimulantsRequest
    {
        public int TestHarnessId { get; set; }
        public TestIndividualCategory TestIndividualCategory { get; set; }
        public int SimulantCount { get; set; }
    }
}