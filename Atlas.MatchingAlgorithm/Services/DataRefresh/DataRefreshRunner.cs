using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
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
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Settings;
using EnumStringValues;

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
        private readonly DataRefreshSettings dataRefreshSettings;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IHlaMetadataDictionary activeVersionHlaMetadataDictionary;
        private readonly IDataRefreshSupportNotificationSender dataRefreshNotificationSender;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly MatchingAlgorithmImportLoggingContext loggingContext;

        private readonly IDonorImportRepository donorImportRepository;

        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;
        private readonly IDonorUpdateProcessor differentialDonorUpdateProcessor;
        private readonly IMatchingAlgorithmImportLogger logger;

        private const string LoggingPrefix = "DATA REFRESH:";

        private readonly List<DataRefreshStage> orderedRefreshStages = EnumExtensions.EnumerateValues<DataRefreshStage>().OrderBy(x => x).ToList();


        private readonly IDictionary<DataRefreshStage, bool> canStageBeSkipped = new Dictionary<DataRefreshStage, bool>
        {
            // We MUST skip the Metadata Refresh step, if we've already progressed past it, as we have to ensure that the Version doesn't change mid-refresh. 
            {DataRefreshStage.MetadataDictionaryRefresh, true},

            // Index removal *must* be skipped for certain continued updates to work.
            // If we have re-created donor HLA Indexes, but then failed later, then we should not delete those Indexes.
            {DataRefreshStage.IndexRemoval, true},

            // Data deletion *must* be skipped for continued updates to work.
            // If we have imported donor data but dropped out during HLA refresh, we should not delete the donor data.
            {DataRefreshStage.DataDeletion, true},

            // Failing to scale up the Database will cause the refresh to take a VERY long time, and it is possible for someone to manually scale the DB back down between interruption and retry.
            // Re-performing this stage if the database is already at the required level is very quick.
            {DataRefreshStage.DatabaseScalingSetup, false},

            // Re-importing of Donors deletion *must* be skipped if we want to continue a partial processing of Donor HLAs, since we need to be certain that the already-processed donors haven't changed underneath us.
            {DataRefreshStage.DonorImport, true},

            // If the step that failed was Index recreation, then we definitely don't want to re-process all the HLA just to do the final steps.
            {DataRefreshStage.DonorHlaProcessing, true},

            // The respective processing times make it pretty unlikely that an interruption would occur after Index recreation completes.
            // But if it *were* to occur then we definitely don't want to have to *re*-re-create them just to do the final 2 steps.
            {DataRefreshStage.IndexRecreation, true},

            // Failing to scale down the Database has a cost impact, and it is possible for someone to manually scale the DB back up between interruption and retry.
            // Re-performing this stage if the database is already at the required level is very quick.
            {DataRefreshStage.DatabaseScalingTearDown, false},

            // Donor updates will still be posted if the refresh quits after this stage. This stage should always be performed last, and the refresh only marked as success when it is fully complete. 
            {DataRefreshStage.QueuedDonorUpdateProcessing, false},
        };

        public DataRefreshRunner(
            DataRefreshSettings dataRefreshSettings,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IAzureDatabaseManager azureDatabaseManager,
            IDormantRepositoryFactory repositoryFactory,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor,
            IDonorUpdateProcessor differentialDonorUpdateProcessor,
            IMatchingAlgorithmImportLogger logger,
            IDataRefreshSupportNotificationSender dataRefreshNotificationSender,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            MatchingAlgorithmImportLoggingContext loggingContext)
        {
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            this.differentialDonorUpdateProcessor = differentialDonorUpdateProcessor;
            this.logger = logger;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.loggingContext = loggingContext;
            this.dataRefreshSettings = dataRefreshSettings;

            // TODO: ATLAS-355: Remove the need for a hardcoded default value
            var hlaVersionOrDefault = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist()
                ? hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion()
                : HlaMetadataDictionaryConstants.NoActiveVersionValue;

            activeVersionHlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaVersionOrDefault);
        }

        public async Task<string> RefreshData(int refreshRecordId)
        {
            try
            {
                var refreshRecord = await dataRefreshHistoryRepository.GetRecord(refreshRecordId);
                var stageExecutionModes = DetermineStageExecutionModes(refreshRecord);

                await RefreshHlaMetadataDictionary(refreshRecord);

                foreach (var dataRefreshStage in orderedRefreshStages.Except(new[] {DataRefreshStage.MetadataDictionaryRefresh}))
                {
                    var executionMode = stageExecutionModes[dataRefreshStage];
                    await ExecuteDataRefreshStage(dataRefreshStage, executionMode, refreshRecord);
                }

                return refreshRecord.HlaNomenclatureVersion;
            }
            catch (Exception ex)
            {
                logger.SendTrace($"{LoggingPrefix} Refresh failed. Exception: {ex}");
                await FailureTearDown(refreshRecordId);
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
                else if (previousStageWasCompletedInInterruptedRun)
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
            var stagesBetweenDbScaling = orderedRefreshStages.Where(stage =>
                stage > DataRefreshStage.DatabaseScalingSetup && stage < DataRefreshStage.DatabaseScalingTearDown);
            var areWeSkippingEveryStageBetweenDbScaling = stagesBetweenDbScaling.All(stage => modes[stage] == DataRefreshStageExecutionMode.Skip);
            if (areWeSkippingEveryStageBetweenDbScaling)
            {
                modes[DataRefreshStage.DatabaseScalingSetup] = DataRefreshStageExecutionMode.Skip;
            }
        }

        // TODO: ATLAS-355: We expect to extract this to somewhere else in the future.
        private async Task RefreshHlaMetadataDictionary(DataRefreshRecord refreshRecord)
        {
            if (string.IsNullOrEmpty(refreshRecord.HlaNomenclatureVersion))
            {
                var newHlaNomenclatureVersion = await activeVersionHlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
                refreshRecord.HlaNomenclatureVersion = newHlaNomenclatureVersion; //Later steps will make use of this value.
                loggingContext.HlaNomenclatureVersion = newHlaNomenclatureVersion;
                await dataRefreshHistoryRepository.UpdateExecutionDetails(refreshRecord.Id, newHlaNomenclatureVersion);
                await dataRefreshHistoryRepository.MarkStageAsComplete(refreshRecord, DataRefreshStage.MetadataDictionaryRefresh);
            }

            // If the Hla version is already populated, then we are continuing an existing run and we already have an HLA Nomenclature for this run.
            // We MUST continue with that same version, (which fortunately we know must already exist, so no need to re-create it)
        }

        private async Task ExecuteDataRefreshStage(
            DataRefreshStage dataRefreshStage,
            DataRefreshStageExecutionMode executionMode,
            DataRefreshRecord refreshRecord)
        {
            switch (executionMode)
            {
                //Note the distinction between `break`s and `return`s here!
                case DataRefreshStageExecutionMode.NotApplicable:
                    logger.SendTrace($"{LoggingPrefix} Stage {dataRefreshStage} is not Applicable to the 'All Stages' execution loop.",
                        LogLevel.Verbose);
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
                    throw new NotImplementedException($"{nameof(DataRefreshStage.MetadataDictionaryRefresh)} is performed separately.");
                case DataRefreshStage.IndexRemoval:
                    await donorImportRepository.RemoveHlaTableIndexes();
                    break;
                case DataRefreshStage.DataDeletion:
                    await donorImportRepository.RemoveAllDonorInformation();
                    break;
                case DataRefreshStage.DatabaseScalingSetup:
                    await ScaleDatabase(dataRefreshSettings.RefreshDatabaseSize.ParseToEnum<AzureDatabaseSize>(), -1);
                    break;
                case DataRefreshStage.DonorImport:
                    if (executionMode == DataRefreshStageExecutionMode.Continuation)
                    {
                        // Resuming mid-donor import is not supported, as we need to ensure that we have consistent
                        // HLA data throughout the whole donor dataset. Instead we will restart the whole stage.
                        await donorImportRepository.RemoveAllDonorInformation();
                    }

                    await donorImporter.ImportDonors(refreshRecord.ShouldMarkAllDonorsAsUpdated);
                    break;
                case DataRefreshStage.DonorHlaProcessing:
                    var isContinuation = (executionMode == DataRefreshStageExecutionMode.Continuation);
                    var verbPhrase = isContinuation ? "Continuing existing processing of" : "Beginning processing of";
                    logger.SendTrace($"{LoggingPrefix} {verbPhrase} Donors using HLA Nomenclature version: {refreshRecord.HlaNomenclatureVersion}");
                    await hlaProcessor.UpdateDonorHla(
                        refreshRecord.HlaNomenclatureVersion,
                        donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecord.Id, donorId),
                        refreshRecord.LastSafelyProcessedDonor,
                        isContinuation);
                    break;
                case DataRefreshStage.IndexRecreation:
                    await donorImportRepository.CreateHlaTableIndexes();
                    break;

                case DataRefreshStage.DatabaseScalingTearDown:
                    await ScaleDatabase(
                        dataRefreshSettings.ActiveDatabaseSize.ParseToEnum<AzureDatabaseSize>(),
                        dataRefreshSettings.ActiveDatabaseAutoPauseTimeout);
                    break;
                case DataRefreshStage.QueuedDonorUpdateProcessing:
                    var dbBeingRefreshed = refreshRecord.Database.ParseToEnum<TransientDatabase>();
                    await differentialDonorUpdateProcessor.ApplyDifferentialDonorUpdatesDuringRefresh(dbBeingRefreshed,
                        refreshRecord.HlaNomenclatureVersion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataRefreshStage), dataRefreshStage, null);
            }

            await dataRefreshHistoryRepository.MarkStageAsComplete(refreshRecord, dataRefreshStage);
        }

        private async Task ScaleDatabase(AzureDatabaseSize targetSize, int? autoPauseDuration = null)
        {
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, targetSize, autoPauseDuration);
        }

        private async Task FailureTearDown(int recordId)
        {
            try
            {
                await ScaleDatabase(
                    dataRefreshSettings.DormantDatabaseSize.ParseToEnum<AzureDatabaseSize>(),
                    dataRefreshSettings.DormantDatabaseAutoPauseTimeout);
            }
            catch (Exception e)
            {
                logger.SendTrace($"{LoggingPrefix} Teardown failed. Database will need scaling down manually. Exception: {e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendTeardownFailureAlert(recordId);
                throw;
            }
        }
    }
}