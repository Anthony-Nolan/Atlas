using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Autofac;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
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
        private static Logger logger;
        private static LoggingConfiguration config;

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            SetupLogging();

            container = CreateContainer();
            var testDataService = container.Resolve<ITestDataService>();

            testDataService.SetupTestData();
            AlgorithmTestingService.StartServer();
            AlgorithmTestingService.RunHlaRefresh();
        }

        private static void SetupLogging()
        {
            config = new LoggingConfiguration();

            var logfileInfo = new FileTarget("logfile") {FileName = "${basedir}\\info.log"};
            var logfileError = new FileTarget("logfile") {FileName = "${basedir}\\error.log"};

            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfileInfo);
            config.AddRule(LogLevel.Error, LogLevel.Fatal, logfileError);

            LogManager.Configuration = config;

            logger = LogManager.GetCurrentClassLogger();
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            AlgorithmTestingService.StopServer();
        }

        [BeforeScenario]
        public static void BeforeScenario()
        {
            var patientDataFactory = container.Resolve<IPatientDataFactory>();

            ScenarioContext.Current.Set(new SearchRequestBuilder());
            ScenarioContext.Current.Set(patientDataFactory);
            ScenarioContext.Current.Set(container.Resolve<IStaticDataProvider>());
            ScenarioContext.Current.Set(container.Resolve<IMultiplePatientDataFactory>());

            // By default, inject the patient data factory as the patient & donor data provider.
            // If using specific test case hla data, this should be overridden in a step definition
            ScenarioContext.Current.Set((IPatientDataProvider) patientDataFactory);
            ScenarioContext.Current.Set((IExpectedDonorProvider) patientDataFactory);
        }

        [AfterScenario]
        public static void AfterScenario()
        {
            LogScenario();
        }

        private static void LogScenario()
        {
            LogManager.Configuration = config;

            ScenarioContext.Current.TryGetValue<SearchAlgorithmApiResult>(out var singlePatientApiResult);
            ScenarioContext.Current.TryGetValue<List<PatientApiResult>>(out var patientApiResults);

            bool.TryParse(ConfigurationManager.AppSettings["log-successful-tests"], out var shouldLogSuccessfulTests);
            var successLogLevel = shouldLogSuccessfulTests ? LogLevel.Info : LogLevel.Off;
            var logLevel = ScenarioContext.Current.TestError == null ? successLogLevel : LogLevel.Error;

            logger.Log(logLevel, ScenarioContext.Current.ScenarioInfo.Title);
            if (singlePatientApiResult != null)
            {
                LogSinglePatientSearchDetails(singlePatientApiResult, logLevel);
            }

            if (patientApiResults != null)
            {
                LogMultiplePatientSearchDetails(patientApiResults, logLevel);
            }
        }

        private static void LogMultiplePatientSearchDetails(IReadOnlyCollection<PatientApiResult> patientApiResults, LogLevel logLevel)
        {
            var factory = ScenarioContext.Current.Get<IMultiplePatientDataFactory>();

            foreach (var patientDataFactory in factory.PatientDataFactories)
            {
                var patientHla = ((IPatientDataProvider) patientDataFactory).GetPatientHla();
                var expectedDonors = ((IExpectedDonorProvider) patientDataFactory).GetExpectedMatchingDonorIds();

                var donors = TestDataRepository.GetDonors(patientApiResults.Single(r => r.ExpectedDonorProvider == patientDataFactory)
                    .ApiResult.Results.SearchResults.Select(r => r.DonorId)).ToList();

                logger.Log(logLevel, "PATIENT HLA:");
                logger.Log(logLevel, JsonConvert.SerializeObject(patientHla));
                logger.Log(logLevel, "EXPECTED DONOR IDS:");
                logger.Log(logLevel, JsonConvert.SerializeObject(expectedDonors));
                logger.Log(logLevel, "DONOR RESULTS:");
                foreach (var match in donors)
                {
                    logger.Log(logLevel, JsonConvert.SerializeObject(match));
                    logger.Log(logLevel, JsonConvert.SerializeObject(donors.Single(d => d.DonorId == match.DonorId)));
                }
            }
        }

        private static void LogSinglePatientSearchDetails(SearchAlgorithmApiResult singlePatientApiResult, LogLevel logLevel)
        {
            var patientHla = ScenarioContext.Current.Get<IPatientDataProvider>().GetPatientHla();
            var expectedDonors = ScenarioContext.Current.Get<IExpectedDonorProvider>().GetExpectedMatchingDonorIds();

            var donors = TestDataRepository.GetDonors(singlePatientApiResult.Results.SearchResults.Select(r => r.DonorId)).ToList();

            logger.Log(logLevel, "PATIENT HLA:");
            logger.Log(logLevel, JsonConvert.SerializeObject(patientHla));
            logger.Log(logLevel, "EXPECTED DONOR IDS:");
            logger.Log(logLevel, JsonConvert.SerializeObject(expectedDonors));
            logger.Log(logLevel, "DONOR RESULTS:");
            foreach (var match in singlePatientApiResult.Results.SearchResults)
            {
                logger.Log(logLevel, JsonConvert.SerializeObject(match));
                logger.Log(logLevel, JsonConvert.SerializeObject(donors.Single(d => d.DonorId == match.DonorId)));
            }
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
            builder.RegisterType<StaticDataProvider>().AsImplementedInterfaces();

            builder.RegisterType<MetaDonorSelector>().AsImplementedInterfaces();
            builder.RegisterType<DatabaseDonorSelector>().AsImplementedInterfaces();
            builder.RegisterType<PatientHlaSelector>().AsImplementedInterfaces();

            return builder.Build();
        }
    }
}