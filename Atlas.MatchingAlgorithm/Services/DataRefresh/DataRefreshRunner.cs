using System;
using System.Collections.Generic;
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

        private readonly List<DataRefreshStage> orderedRefreshStages = EnumExtensions.EnumerateValues<DataRefreshStage>().OrderBy(x => x).ToList();


        private readonly IDictionary<DataRefreshStage, bool> canStageBeSkipped = new Dictionary<DataRefreshStage, bool>
        {
            // Can not skip metadata dictionary refresh, as it is required to fetch the latest nomenclature version.
            {DataRefreshStage.MetadataDictionaryRefresh, false},

            // Data deletion *must* be skipped for continued updates to work.
            // If we have imported donor data but dropped out during HLA refresh, we should not delete the donor data.
            {DataRefreshStage.DataDeletion, true},

            // Failing to scale up the Database will cause the refresh to take a VERY long time, and it is possible for someone to manually scale the DB back down between interruption and retry.
            // Re-performing this stage if the database is already at the required level is very quick.
            {DataRefreshStage.DatabaseScalingSetup, false},

            // Re-importing of Donors deletion *must* be skipped if we want to continue a partial processing of Donor HLAs, since we need to be certain that the already-processed donors haven't changed underneath us.
            {DataRefreshStage.DonorImport, true},

            // The respective processing times make it pretty unlikely that an interruption would occur after HLA processing completes.
            // But if it were to occur then we definitely don't want to re-process all the HLA just to do the final 2 steps.
            {DataRefreshStage.DonorHlaProcessing, true},

            // Failing to scale down the Database has a cost impact, and it is possible for someone to manually scale the DB back up between interruption and retry.
            // Re-performing this stage if the database is already at the required level is very quick.
            {DataRefreshStage.DatabaseScalingTearDown, false},

            // Donor updates will still be posted if the refresh quits after this stage. This stage should always be performed last, and the refresh only marked as success when it is fully complete. 
            {DataRefreshStage.QueuedDonorUpdateProcessing, false},
        };

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
                var refreshRecord = await dataRefreshHistoryRepository.GetRecord(refreshRecordId);

                var stageExecutionModes = DetermineStageExecutionModes(refreshRecord);
                foreach (var dataRefreshStage in orderedRefreshStages)
                {
                    var executionMode = stageExecutionModes[dataRefreshStage];
                    await ExecuteDataRefreshStage(dataRefreshStage, executionMode, refreshRecord.Id, newHlaNomenclatureVersion);
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

        /// <remarks>
        /// We assume that progress is always linear.
        /// Thus (ignoring skipability of steps) our data will look something like this:
        ///  F = Finished.
        ///  N = Not Finished.
        ///  S = Skip
        ///  C = Continue.
        ///  R = Run
        ///
        /// Previous: F F F N N N N
        /// Current:  S S S C R R R
        /// </remarks>
        private Dictionary<DataRefreshStage, DataRefreshStageExecutionMode> DetermineStageExecutionModes(DataRefreshRecord refreshRecord)
        {
            var modes = new Dictionary<DataRefreshStage, DataRefreshStageExecutionMode>();

            var previousStageWasCompletedInInterruptedRun = false; //For the first stage there is no "previous stage".
            foreach (var stage in orderedRefreshStages)
            {
                var currentStageWasCompletedInPreviousRun = refreshRecord.IsStageComplete(stage);
                if (currentStageWasCompletedInPreviousRun)
                {
                    modes[stage] = canStageBeSkipped[stage]
                        ? DataRefreshStageExecutionMode.Skip
                        : DataRefreshStageExecutionMode.FromScratch;
                }
                else if(previousStageWasCompletedInInterruptedRun)
                {
                    modes[stage] = DataRefreshStageExecutionMode.Continuation;
                }
                else
                {
                    modes[stage] = DataRefreshStageExecutionMode.FromScratch;
                }
                previousStageWasCompletedInInterruptedRun = currentStageWasCompletedInPreviousRun;
            }

            AvoidScalingDbUpAndImmediatelyBackDown(modes);

            modes[DataRefreshStage.MetadataDictionaryRefresh] = DataRefreshStageExecutionMode.NotApplicable; //This step is performed separately from the other steps, prior to determining and running the other stages TODO: ATLAS-355. Remove this special case.
            modes[DataRefreshStage.QueuedDonorUpdateProcessing] = DataRefreshStageExecutionMode.NotApplicable; //This step is not yet implemented.  TODO: ATLAS-249. Remove this special case.

            return modes;
        }

        /// <summary>
        /// In general we don't skip the DB scaling steps, because we expect them to either be necessary or quick.
        /// There's an edge case, however, if we were going to be Skipping ALL of the stages between the scaling, and the DB is currently scaled up.
        /// In that case we'd scale it up and then immediately scale it back down. Not causing any PROBLEMS, but wasting a bunch of time!
        /// If we detect that's the case, we can save the time by skipping the scale up.
        /// We still scale it down though, because the DB might have been left in a ScaledUp state, in which case it's really bad if we *leave* it up.
        /// </summary>
        private void AvoidScalingDbUpAndImmediatelyBackDown(Dictionary<DataRefreshStage, DataRefreshStageExecutionMode> modes)
        {
            var stagesBetweenDBScaling = orderedRefreshStages.Where(stage => stage > DataRefreshStage.DatabaseScalingSetup && stage < DataRefreshStage.DatabaseScalingTearDown);
            var areWeSkippingEveryStageBetweenDbScaling = stagesBetweenDBScaling.All(stage => modes[stage] == DataRefreshStageExecutionMode.Skip);
            if (areWeSkippingEveryStageBetweenDbScaling)
            {
                modes[DataRefreshStage.DatabaseScalingSetup] = DataRefreshStageExecutionMode.Skip;
            }
        }

        private async Task<string> RefreshHlaMetadataDictionary(int refreshRecordId)
        {
            // TODO: ATLAS-355: Move this code somewhere independent from the Donor Data Refresh.

            // Hla Metadata Dictionary Refresh is not performed atomically as the resulting nomenclature version is needed in other stages.
            var newHlaNomenclatureVersion = await activeVersionHlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
            await dataRefreshHistoryRepository.UpdateExecutionDetails(refreshRecordId, newHlaNomenclatureVersion);
            await dataRefreshHistoryRepository.MarkStageAsComplete(refreshRecordId, DataRefreshStage.MetadataDictionaryRefresh);
            return newHlaNomenclatureVersion;
        }

        private async Task ExecuteDataRefreshStage(
            DataRefreshStage dataRefreshStage,
            DataRefreshStageExecutionMode executionMode,
            int refreshRecordId,
            string newHlaNomenclatureVersion)
        {
            switch (executionMode)
            {
                //Note the distinction between `break`s and `return`s here!
                case DataRefreshStageExecutionMode.NotApplicable:
                    logger.SendTrace($"{LoggingPrefix} Stage {dataRefreshStage} is not Applicable to the atomic execution loop.", LogLevel.Verbose);
                    return;
                case DataRefreshStageExecutionMode.Skip:
                    logger.SendTrace($"{LoggingPrefix} Stage {dataRefreshStage} is already complete and can be skipped. Skipping.");
                    return;
                case DataRefreshStageExecutionMode.Continuation:
                    logger.SendTrace($"{LoggingPrefix} Attempting to Continue Stage {dataRefreshStage} from a previous execution.");
                    break;
                case DataRefreshStageExecutionMode.FromScratch:
                    logger.SendTrace($"{LoggingPrefix} Running Stage {dataRefreshStage} for the first time.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(executionMode), executionMode, null);
            }

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
                    if (executionMode == DataRefreshStageExecutionMode.Continuation)
                    {
                        // Resuming mid-donor import is not supported, instead we will restart the whole stage.
                        await donorImportRepository.RemoveAllDonorInformation();
                    }
                    await donorImporter.ImportDonors();
                    break;
                case DataRefreshStage.DonorHlaProcessing:
                    logger.SendTrace($"{LoggingPrefix} Using HLA Nomenclature version: {newHlaNomenclatureVersion}");
                    if (executionMode == DataRefreshStageExecutionMode.Continuation)
                    {
                        // TODO: ATLAS-251: Allow continuation mid-hla processing.
                        // For now, resuming mid-hla processing is not yet supported, instead we will restart the whole stage.
                        await donorImportRepository.RemoveAllProcessedDonorHla();
                    }
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