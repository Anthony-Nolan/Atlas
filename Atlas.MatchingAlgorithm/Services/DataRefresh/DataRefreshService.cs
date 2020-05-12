using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.Utils.Core.ApplicationInsights;
using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;

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
        /// <returns>The version of the Wmda database used</returns>
        Task<string> RefreshData();
    }

    public class DataRefreshService : IDataRefreshService
    {
        private readonly IOptions<DataRefreshSettings> settingsOptions;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
        private readonly IDataRefreshNotificationSender dataRefreshNotificationSender;

        private readonly IDonorImportRepository donorImportRepository;

        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;
        private readonly ILogger logger;

        public DataRefreshService(
            IOptions<DataRefreshSettings> dataRefreshSettingsOptions,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IAzureDatabaseManager azureDatabaseManager,
            IDormantRepositoryFactory repositoryFactory,
            IHlaMetadataDictionary hlaMetadataDictionary,
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor,
            ILogger logger,
            IDataRefreshNotificationSender dataRefreshNotificationSender)
        {
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            this.hlaMetadataDictionary = hlaMetadataDictionary;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            this.logger = logger;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
            settingsOptions = dataRefreshSettingsOptions;
        }

        public async Task<string> RefreshData()
        {
            try
            {
                var newHlaDatabaseVersion = await RecreateHlaMetadataDictionary();
                await RemoveExistingDonorData();
                await ScaleDatabase(settingsOptions.Value.RefreshDatabaseSize.ToAzureDatabaseSize());
                await ImportDonors();
                await ProcessDonorHla(newHlaDatabaseVersion);
                await ScaleDatabase(settingsOptions.Value.ActiveDatabaseSize.ToAzureDatabaseSize());

                return newHlaDatabaseVersion;
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

        private async Task<string> RecreateHlaMetadataDictionary()
        {
            logger.SendTrace($"DATA REFRESH: Recreating HLA Metadata dictionary from latest WMDA database version.", LogLevel.Info);
            var wmdaDatabaseVersion = await hlaMetadataDictionary.RecreateHlaMetadataDictionary(MatchingDictionary.HlaMetadataDictionary.CreationBehaviour.Latest);
            logger.SendTrace($"DATA REFRESH: HLA Metadata dictionary recreated at version: {wmdaDatabaseVersion}", LogLevel.Info);
            return wmdaDatabaseVersion;
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