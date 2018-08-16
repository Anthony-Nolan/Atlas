using Autofac;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public sealed class Hooks
    {
        private static IContainer container;

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            container = CreateContainer();
            var testDataService = container.Resolve<ITestDataService>();
            
            testDataService.SetupTestData();
            AlgorithmTestingService.StartServer();
            AlgorithmTestingService.RunHlaRefresh();
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            AlgorithmTestingService.StopServer();
        }

        [BeforeScenario]
        public static void BeforeScenario()
        {
            ScenarioContext.Current.Set(new SearchRequestBuilder());
            ScenarioContext.Current.Set(container.Resolve<IPatientDataSelector>());
        }
        
        private static IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            
            builder.RegisterType<MetaDonorRepository>().AsImplementedInterfaces();
            builder.RegisterType<AlleleRepository>().AsImplementedInterfaces();
            
            builder.RegisterType<TestDataService>().AsImplementedInterfaces();
            
            builder.RegisterType<PatientDataSelector>().AsImplementedInterfaces();
            builder.RegisterType<MetaDonorSelector>().AsImplementedInterfaces();

            return builder.Build();
        }
    }
}
