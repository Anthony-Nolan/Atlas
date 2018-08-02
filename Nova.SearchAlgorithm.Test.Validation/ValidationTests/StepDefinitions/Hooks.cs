using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public sealed class Hooks
    {
        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            AlgorithmService.StartServer();
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            AlgorithmService.StopServer();
        }
        
    }
}
