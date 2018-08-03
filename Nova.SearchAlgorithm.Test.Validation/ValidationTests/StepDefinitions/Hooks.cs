using Nova.SearchAlgorithm.Data;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public sealed class Hooks
    {
        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            TestDataGenerator.SetupDatabase();
            TestDataGenerator.AddTestDonors();
            AlgorithmTestingService.StartServer();
            AlgorithmTestingService.RunHlaRefresh();
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            AlgorithmTestingService.StopServer();
        }
        
    }
}
