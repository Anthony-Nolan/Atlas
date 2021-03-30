using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.MatchingAlgorithm.Test.Validation.DependencyInjection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.StaticDataSelection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using TechTalk.SpecFlow;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public sealed class Hooks
    {
        private static IServiceProvider serviceProvider;
        private static Logger logger;
        private static LoggingConfiguration config;
        private readonly ScenarioContext scenarioContext;

        public Hooks(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [BeforeTestRun]
        public static async Task BeforeTestRun()
        {
            try
            {
                serviceProvider = ServiceConfiguration.CreateProvider();
                
                SetupLogging();

                var testDataService = serviceProvider.GetService<ITestDataService>();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                testDataService.SetupTestData();

                logger.Log(LogLevel.Info, $"Set up test data: {stopwatch.Elapsed}");
                stopwatch.Restart();

                AlgorithmTestingService.StartServer();
                logger.Log(LogLevel.Info, $"Start test server: {stopwatch.Elapsed}");
                stopwatch.Restart();

                await AlgorithmTestingService.RunHlaRefresh();
                logger.Log(LogLevel.Info, $"Run HLA refresh: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
            catch (Exception e)
            {
                //Unfortunately the Test Logging for errors in here is awful :( so we have to do this hideous abuse to get anything interprettable.
                throw new Exception(e.ToString(), e);
            }
        }

        private static void SetupLogging()
        {
            config = new LoggingConfiguration();

            var baseDirectory = "${basedir}\\" + OptionsReaderFor<ValidationTestSettings>()(serviceProvider).LogFileDirectory;

            var traceFileInfo = new FileTarget("logfile") {FileName = $"{baseDirectory}\\trace.log", ArchiveOldFileOnStartup = true, MaxArchiveDays = 2};
            var logfileInfo = new FileTarget("logfile") {FileName = $"{baseDirectory}\\info.log", ArchiveOldFileOnStartup = true, MaxArchiveDays = 2};
            var logfileError = new FileTarget("logfile") {FileName = $"{baseDirectory}\\error.log", ArchiveOldFileOnStartup = true, MaxArchiveDays = 2};

            config.AddRule(LogLevel.Trace, LogLevel.Trace, traceFileInfo);
            config.AddRule(LogLevel.Info, LogLevel.Info, logfileInfo);
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
        public void BeforeScenario()
        {
            var patientDataFactory = serviceProvider.GetService<IPatientDataFactory>();

            scenarioContext.Set(new SearchRequestBuilder());
            scenarioContext.Set(patientDataFactory);
            scenarioContext.Set(serviceProvider.GetService<IStaticDataProvider>());
            scenarioContext.Set(serviceProvider.GetService<IMultiplePatientDataFactory>());

            // By default, inject the patient data factory as the patient & donor data provider.
            // If using specific test case hla data, this should be overridden in a step definition
            scenarioContext.Set((IPatientDataProvider) patientDataFactory);
            scenarioContext.Set((IExpectedDonorProvider) patientDataFactory);
        }

        [AfterScenario]
        public void AfterScenario()
        {
            LogScenario();
        }

        private void LogScenario()
        {
            LogManager.Configuration = config;

            scenarioContext.TryGetValue<SearchAlgorithmApiResult>(out var singlePatientApiResult);
            scenarioContext.TryGetValue<List<PatientApiResult>>(out var patientApiResults);

            var shouldLogSuccessfulTests = OptionsReaderFor<ValidationTestSettings>()(serviceProvider).LogSuccessfulTests;
            var successLogLevel = shouldLogSuccessfulTests ? LogLevel.Info : LogLevel.Off;
            var logLevel = scenarioContext.TestError == null ? successLogLevel : LogLevel.Error;

            logger.Log(logLevel, scenarioContext.ScenarioInfo.Title);
            if (singlePatientApiResult != null)
            {
                LogSinglePatientSearchDetails(singlePatientApiResult, logLevel);
            }

            if (patientApiResults != null)
            {
                LogMultiplePatientSearchDetails(patientApiResults, logLevel);
            }
        }

        private void LogMultiplePatientSearchDetails(IReadOnlyCollection<PatientApiResult> patientApiResults, LogLevel logLevel)
        {
            var factory = scenarioContext.Get<IMultiplePatientDataFactory>();

            foreach (var patientDataFactory in factory.PatientDataFactories)
            {
                var patientHla = ((IPatientDataProvider) patientDataFactory).GetPatientHla();
                var expectedDonors = ((IExpectedDonorProvider) patientDataFactory).GetExpectedMatchingDonorIds();

                var donors = serviceProvider.GetService<ITestDataRepository>()
                    .GetDonors(patientApiResults.Single(r =>
                        r.ExpectedDonorProvider == patientDataFactory).ApiResult.Results.Results.Select(r => r.AtlasDonorId))
                    .ToList();

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

        private void LogSinglePatientSearchDetails(SearchAlgorithmApiResult singlePatientApiResult, LogLevel logLevel)
        {
            var patientHla = scenarioContext.Get<IPatientDataProvider>().GetPatientHla();
            var expectedDonors = scenarioContext.Get<IExpectedDonorProvider>().GetExpectedMatchingDonorIds();

            var donors = serviceProvider.GetService<ITestDataRepository>()
                .GetDonors(singlePatientApiResult.Results.Results.Select(r => r.AtlasDonorId)).ToList();

            logger.Log(logLevel, "PATIENT HLA:");
            logger.Log(logLevel, JsonConvert.SerializeObject(patientHla));
            logger.Log(logLevel, "EXPECTED DONOR IDS:");
            logger.Log(logLevel, JsonConvert.SerializeObject(expectedDonors));
            logger.Log(logLevel, "DONOR RESULTS:");
            foreach (var match in singlePatientApiResult.Results.Results)
            {
                logger.Log(logLevel, JsonConvert.SerializeObject(match));
                logger.Log(logLevel, JsonConvert.SerializeObject(donors.Single(d => d.DonorId == match.AtlasDonorId)));
            }
        }
    }
}