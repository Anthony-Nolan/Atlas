using Atlas.MatchPrediction.Test.Verification.Data.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration
{
    public class GenerateSimulantsRequest
    {
        public int TestHarnessId { get; set; }
        public TestIndividualCategory TestIndividualCategory { get; set; }
        public int SimulantCount { get; set; }
    }
}