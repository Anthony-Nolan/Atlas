using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Settings;
using EnumStringValues;
using Microsoft.Extensions.Options;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshRunner
    {
        /// <summary>
        /// Performs all pre-processing required for running of the search algorithm:
        /// - Scales up target database 
        /// - Recreates HlaMetadata Dictionary
        /// - Imports all donors
        /// - Processes HLA for imported donors
        /// - Scales down target database
        /// </summary>
        /// <returns>The version of the HLA Nomenclature used for the new data</returns>
        Task<string> RefreshData(int refreshRecordId);
    }

    public class DataRefreshRunner : IDataRefreshRunner
    {
        private readonly IOptions<DataRefreshSettings> settingsOptions;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IHlaMetadataDictionary activeVersionHlaMetadataDictionary;
        private readonly IDataRefreshNotificationSender dataRefreshNotificationSender;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private readonly IDonorImportRepository donorImportRepository;

        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;
        private readonly ILogger logger;

        public DataRefreshRunner(
            IOptions<DataRefreshSettings> dataRefreshSettingsOptions,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IAzureDatabaseManager azureDatabaseManager,
            IDormantRepositoryFactory repositoryFactory,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor,
            ILogger logger,
            IDataRefreshNotificationSender dataRefreshNotificationSender,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository)
        {
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            this.logger = logger;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            settingsOptions = dataRefreshSettingsOptions;

            // TODO: ATLAS-355: Remove the need for a hardcoded default value
            activeVersionHlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(
                hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersionOrDefault()
            );
        }

        public async Task<string> RefreshData(int refreshRecordId)
        {
            try
            {
                // Hla Metadata Dictionary Refresh is not performed atomically as the resulting nomenclature version is needed in other stages.
                var newHlaNomenclatureVersion = await activeVersionHlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
                await dataRefreshHistoryRepository.MarkStageAsComplete(refreshRecordId, DataRefreshStage.MetadataDictionaryRefresh);
                var orderedRefreshStages = EnumExtensions.EnumerateValues<DataRefreshStage>()
                    .Except(new[] {DataRefreshStage.MetadataDictionaryRefresh})
                    // TODO: ATLAS-249: Implement new donor update workflow
                    .Except(new[] {DataRefreshStage.QueuedDonorUpdateProcessing})
                    .OrderBy(x => x);

                foreach (var dataRefreshStage in orderedRefreshStages)
                {
                    await RunDataRefreshStage(dataRefreshStage, refreshRecordId, newHlaNomenclatureVersion);
                }

                return newHlaNomenclatureVersion;
            }
            catch (Exception ex)
            {
                logger.SendTrace($"DATA REFRESH: Refresh failed. Exception: {ex}", LogLevel.Info);
                await FailureTearDown();
                throw;
            }
        }

        private async Task RunDataRefreshStage(DataRefreshStage dataRefreshStage, int refreshRecordId, string newHlaNomenclatureVersion)
        {
            switch (dataRefreshStage)
            {
                case DataRefreshStage.MetadataDictionaryRefresh:
                    throw new NotImplementedException($"{nameof(DataRefreshStage.MetadataDictionaryRefresh)} cannot be performed atomically.");
                case DataRefreshStage.DataDeletion:
                    await RemoveExistingDonorData(refreshRecordId);
                    break;
                case DataRefreshStage.DatabaseScalingSetup:
                    await ScaleDatabase(settingsOptions.Value.RefreshDatabaseSize.ParseToEnum<AzureDatabaseSize>());
                    break;
                case DataRefreshStage.DonorImport:
                    await ImportDonors(refreshRecordId);
                    break;
                case DataRefreshStage.DonorHlaProcessing:
                    await ProcessDonorHla(newHlaNomenclatureVersion, refreshRecordId);
                    break;
                case DataRefreshStage.DatabaseScalingTearDown:
                    await ScaleDatabase(settingsOptions.Value.ActiveDatabaseSize.ParseToEnum<AzureDatabaseSize>());
                    break;
                case DataRefreshStage.QueuedDonorUpdateProcessing:
                    // TODO: ATLAS-249: Implement new donor update workflow
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataRefreshStage), dataRefreshStage, null);
            }

            await dataRefreshHistoryRepository.MarkStageAsComplete(refreshRecordId, dataRefreshStage);
        }

        private async Task FailureTearDown()
        {
            try
            {
                await ScaleDatabase(settingsOptions.Value.DormantDatabaseSize.ParseToEnum<AzureDatabaseSize>());
            }
            catch (Exception e)
            {
                logger.SendTrace($"DATA REFRESH: Teardown failed. Database will need scaling down manually. Exception: {e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendTeardownFailureAlert();
                throw;
            }
        }

        private async Task RemoveExistingDonorData(int refreshRecordId)
        {
            logger.SendTrace("DATA REFRESH: Removing existing donor data", LogLevel.Info);
            await donorImportRepository.RemoveAllDonorInformation();
        }

        private async Task ImportDonors(int refreshRecordId)
        {
            logger.SendTrace("DATA REFRESH: Importing Donors", LogLevel.Info);
            await donorImporter.ImportDonors();
        }

        private async Task ProcessDonorHla(string hlaNomenclatureVersion, int refreshRecordId)
        {
            logger.SendTrace($"DATA REFRESH: Processing Donor hla using HLA Nomenclature version: {hlaNomenclatureVersion}", LogLevel.Info);
            await hlaProcessor.UpdateDonorHla(hlaNomenclatureVersion);
        }

        private async Task ScaleDatabase(AzureDatabaseSize targetSize)
        {
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            logger.SendTrace($"DATA REFRESH: Scaling database: {databaseName} to size {targetSize}", LogLevel.Info);
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, targetSize);
        }
    }
}