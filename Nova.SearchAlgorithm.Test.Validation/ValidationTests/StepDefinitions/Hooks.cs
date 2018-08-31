using Autofac;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
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
            ScenarioContext.Current.Set(container.Resolve<IPatientDataFactory>());
            ScenarioContext.Current.Set(container.Resolve<IMultiplePatientDataFactory>());
        }
        
        private static IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            
            // As some of the meta donors are generated dynamically at runtime, the repository must be a singleton
            // Otherwise, the meta-donors will be regenerated on lookup, and no longer match the ones in the database
            builder.RegisterType<MetaDonorsData>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MetaDonorRepository>().AsImplementedInterfaces().SingleInstance();
            
            builder.RegisterType<AlleleRepository>().AsImplementedInterfaces();
            
            builder.RegisterType<TestDataService>().AsImplementedInterfaces();
            
            builder.RegisterType<PatientDataFactory>().AsImplementedInterfaces();
            builder.RegisterType<MultiplePatientDataFactory>().AsImplementedInterfaces();
            
            builder.RegisterType<MetaDonorSelector>().AsImplementedInterfaces();
            builder.RegisterType<DatabaseDonorSelector>().AsImplementedInterfaces();
            builder.RegisterType<PatientHlaSelector>().AsImplementedInterfaces();

            return builder.Build();
        }
    }
}
