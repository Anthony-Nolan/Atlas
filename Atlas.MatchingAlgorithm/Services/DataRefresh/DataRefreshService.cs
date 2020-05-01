using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.Utils.Core.ApplicationInsights;
using System;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshService
    {
        /// <summary>
        /// Performs all pre-processing required for running of the search algorithm:
        /// - Scales up target database 
        /// - Recreates Matching Dictionary
        /// - Imports all donors
        /// - Processes HLA for imported donors
        /// - Scales down target database
        /// </summary>
        /// <param name="wmdaDatabaseVersion">The version of the wmda hla database to use for this refresh</param>
        /// <param name="isContinuedRefresh">
        /// If true, continues a data refresh where it left off, without removing all donor information
        /// This relies on the individual steps of the refresh process bring resilient to interruption
        /// </param>
        Task RefreshData(string wmdaDatabaseVersion, bool isContinuedRefresh = false);
    }

    public class DataRefreshService : IDataRefreshService
    {
        private readonly IOptions<DataRefreshSettings> settingsOptions;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IDataRefreshNotificationSender dataRefreshNotificationSender;

        private readonly IDonorImportRepository donorImportRepository;

        private readonly IRecreateHlaLookupResultsService recreateMatchingDictionaryService;
        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;
        private readonly ILogger logger;

        public DataRefreshService(
            IOptions<DataRefreshSettings> dataRefreshSettingsOptions,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IAzureDatabaseManager azureDatabaseManager,
            IDormantRepositoryFactory repositoryFactory,
            IRecreateHlaLookupResultsService recreateMatchingDictionaryService,
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor,
            ILogger logger,
            IDataRefreshNotificationSender dataRefreshNotificationSender)
        {
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            this.recreateMatchingDictionaryService = recreateMatchingDictionaryService;
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            this.logger = logger;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
            settingsOptions = dataRefreshSettingsOptions;
        }

        public async Task RefreshData(string wmdaDatabaseVersion, bool isContinuedRefresh)
        {
            try
            {
                await RecreateMatchingDictionary(wmdaDatabaseVersion);
                if (!isContinuedRefresh)
                {
                    await RemoveExistingDonorData();
                }
                await ScaleDatabase(settingsOptions.Value.RefreshDatabaseSize.ToAzureDatabaseSize());
                await ImportDonors();
                await ProcessDonorHla(wmdaDatabaseVersion);
                await ScaleDatabase(settingsOptions.Value.ActiveDatabaseSize.ToAzureDatabaseSize());
            }
            catch (Exception ex)
            {
                logger.SendTrace($"DATA REFRESH: Refresh failed. Exception: {ex}", LogLevel.Info);
                await FailureTearDown();
                throw;
            }
        }

        private async Task FailureTearDown()
        {
            try
            {
                await ScaleDatabase(settingsOptions.Value.DormantDatabaseSize.ToAzureDatabaseSize());
            }
            catch (Exception e)
            {
                logger.SendTrace($"DATA REFRESH: Teardown failed. Database will need scaling down manually. Exception: {e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendTeardownFailureAlert();
                throw;
            }
        }

        private async Task RecreateMatchingDictionary(string wmdaDatabaseVersion)
        {
            logger.SendTrace($"DATA REFRESH: Recreating matching dictionary for hla database version: {wmdaDatabaseVersion}", LogLevel.Info);
            await recreateMatchingDictionaryService.RecreateAllHlaLookupResults(wmdaDatabaseVersion);
        }

        private async Task RemoveExistingDonorData()
        {
            logger.SendTrace("DATA REFRESH: Removing existing donor data", LogLevel.Info);
            await donorImportRepository.RemoveAllDonorInformation();
        }

        private async Task ImportDonors()
        {
            logger.SendTrace("DATA REFRESH: Importing Donors", LogLevel.Info);
            await donorImporter.ImportDonors();
        }

        private async Task ProcessDonorHla(string wmdaDatabaseVersion)
        {
            logger.SendTrace($"DATA REFRESH: Processing Donor hla using hla database version: {wmdaDatabaseVersion}", LogLevel.Info);
            await hlaProcessor.UpdateDonorHla(wmdaDatabaseVersion);
        }

        private async Task ScaleDatabase(AzureDatabaseSize targetSize)
        {
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            logger.SendTrace($"DATA REFRESH: Scaling database: {databaseName} to size {targetSize}", LogLevel.Info);
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, targetSize);
        }
    }
}