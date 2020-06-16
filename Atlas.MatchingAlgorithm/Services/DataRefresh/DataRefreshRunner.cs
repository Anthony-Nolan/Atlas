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

        private const string LoggingPrefix = "DATA REFRESH:";

        private readonly IOrderedEnumerable<DataRefreshStage> orderedRefreshStages = EnumExtensions.EnumerateValues<DataRefreshStage>()
            .Except(new[] {DataRefreshStage.MetadataDictionaryRefresh})
            .Except(new[] {DataRefreshStage.QueuedDonorUpdateProcessing}) // TODO: ATLAS-249: Implement new donor update workflow
            .OrderBy(x => x);

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
                var newHlaNomenclatureVersion = await RefreshHlaMetadataDictionary(refreshRecordId);

                foreach (var dataRefreshStage in orderedRefreshStages)
                {
                    await RunDataRefreshStage(dataRefreshStage, refreshRecordId, newHlaNomenclatureVersion);
                }

                return newHlaNomenclatureVersion;
            }
            catch (Exception ex)
            {
                logger.SendTrace($"{LoggingPrefix} Refresh failed. Exception: {ex}", LogLevel.Info);
                await FailureTearDown();
                throw;
            }
        }

        private async Task<string> RefreshHlaMetadataDictionary(int refreshRecordId)
        {
            // Hla Metadata Dictionary Refresh is not performed atomically as the resulting nomenclature version is needed in other stages.
            var newHlaNomenclatureVersion = await activeVersionHlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
            await dataRefreshHistoryRepository.MarkStageAsComplete(refreshRecordId, DataRefreshStage.MetadataDictionaryRefresh);
            return newHlaNomenclatureVersion;
        }

        private async Task RunDataRefreshStage(DataRefreshStage dataRefreshStage, int refreshRecordId, string newHlaNomenclatureVersion)
        {
            logger.SendTrace($"{LoggingPrefix} Running stage {dataRefreshStage}", LogLevel.Info);

            switch (dataRefreshStage)
            {
                case DataRefreshStage.MetadataDictionaryRefresh:
                    throw new NotImplementedException($"{nameof(DataRefreshStage.MetadataDictionaryRefresh)} cannot be performed atomically.");
                case DataRefreshStage.DataDeletion:
                    await donorImportRepository.RemoveAllDonorInformation();
                    break;
                case DataRefreshStage.DatabaseScalingSetup:
                    await ScaleDatabase(settingsOptions.Value.RefreshDatabaseSize.ParseToEnum<AzureDatabaseSize>());
                    break;
                case DataRefreshStage.DonorImport:
                    await donorImporter.ImportDonors();
                    break;
                case DataRefreshStage.DonorHlaProcessing:
                    logger.SendTrace($"{LoggingPrefix} Using HLA Nomenclature version: {newHlaNomenclatureVersion}", LogLevel.Info);
                    await hlaProcessor.UpdateDonorHla(newHlaNomenclatureVersion);
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

        private async Task ScaleDatabase(AzureDatabaseSize targetSize)
        {
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, targetSize);
        }

        private async Task FailureTearDown()
        {
            try
            {
                await ScaleDatabase(settingsOptions.Value.DormantDatabaseSize.ParseToEnum<AzureDatabaseSize>());
            }
            catch (Exception e)
            {
                logger.SendTrace($"{LoggingPrefix} Teardown failed. Database will need scaling down manually. Exception: {e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendTeardownFailureAlert();
                throw;
            }
        }
    }
}