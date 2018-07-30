using Nova.SearchAlgorithm.Services.Scoring.Grading;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
{
    public class GradingServiceTests
    {
        private IGradingService gradingService;

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            gradingService = new GradingService();
        }
    }
}
