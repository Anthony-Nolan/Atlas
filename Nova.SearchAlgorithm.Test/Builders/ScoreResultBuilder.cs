using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Test.Builders
{
    public class ScoreResultBuilder
    {
        private readonly ScoreResult scoreResult;

        public ScoreResultBuilder()
        {
            scoreResult = new ScoreResult();
        }

        public ScoreResult Build()
        {
            return scoreResult;
        }
    }
}