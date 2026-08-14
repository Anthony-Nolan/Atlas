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
using Atlas.MatchingAlgorithm.Data.Settings;
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
using Microsoft.Data.SqlClient;

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
        private readonly IDataRefreshRuntimeSampler runtimeSampler;

        private const string LoggingPrefix = "DATA REFRESH:";

        /// <summary>
        /// Name of the customEvent carrying the run manifest. A run that cannot describe its own configuration cannot
        /// be compared with another run, so this is emitted before any work starts.
        /// </summary>
        internal const string RunManifestEventName = "Data Refresh Run Manifest";

        private readonly List<DataRefreshStage> orderedRefreshStages = EnumExtensions.EnumerateValues<DataRefreshStage>().OrderBy(x => x).ToList();


        private readonly IDictionary<DataRefreshStage, bool> canStageBeSkipped = new Dictionary<DataRefreshStage, bool>
        {
            // We MUST skip the Metadata Refresh step, if we've already progressed past it, as we have to ensure that the Version doesn't change mid-refresh. 
            { DataRefreshStage.MetadataDictionaryRefresh, true },

            // Index removal *must* be skipped for certain continued updates to work.
            // If we have re-created donor HLA Indexes, but then failed later, then we should not delete those Indexes.
            { DataRefreshStage.IndexRemoval, true },

            // Data deletion *must* be skipped for continued updates to work.
            // If we have imported donor data but dropped out during HLA refresh, we should not delete the donor data.
            // Note: DonorImport writes donor management log entries create-only, on the assumption that this stage has emptied that table
            // beforehand. Skipping this stage is only safe because DonorImport is itself either skipped, or restarted from scratch with its own
            // deletion, whenever this stage is skipped. See the DonorImport case in ExecuteDataRefreshStage.
            { DataRefreshStage.DataDeletion, true },

            // Failing to scale up the Database will cause the refresh to take a VERY long time, and it is possible for someone to manually scale the DB back down between interruption and retry.
            // Re-performing this stage if the database is already at the required level is very quick.
            { DataRefreshStage.DatabaseScalingSetup, false },

            // Re-importing of Donors deletion *must* be skipped if we want to continue a partial processing of Donor HLAs, since we need to be certain that the already-processed donors haven't changed underneath us.
            { DataRefreshStage.DonorImport, true },

            // If the step that failed was Index recreation, then we definitely don't want to re-process all the HLA just to do the final steps.
            { DataRefreshStage.DonorHlaProcessing, true },

            // The respective processing times make it pretty unlikely that an interruption would occur after Index recreation completes.
            // But if it *were* to occur then we definitely don't want to have to *re*-re-create them just to do the final 2 steps.
            { DataRefreshStage.IndexRecreation, true },

            // Donor updates will still be posted if the refresh quits after this stage, so it must always be re-performed on a continuation,
            // and the refresh only marked as success once every stage has completed.
            { DataRefreshStage.QueuedDonorUpdateProcessing, false },

            // Failing to scale down the Database has a cost impact, and it is possible for someone to manually scale the DB back up between interruption and retry.
            // Re-performing this stage if the database is already at the required level is very quick.
            { DataRefreshStage.DatabaseScalingTearDown, false },
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
            MatchingAlgorithmImportLoggingContext loggingContext,
            IDataRefreshRuntimeSampler runtimeSampler)
        {
            this.runtimeSampler = runtimeSampler;
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
            DataRefreshStage? currentStage = null;

            // Started here and disposed in the finally, so utilisation is sampled across every exit path - including
            // the failure path, which is precisely when knowing what the process was doing matters most.
            await using var runtimeSampling = runtimeSampler.StartSampling();

            try
            {
                var refreshRecord = await dataRefreshHistoryRepository.GetRecord(refreshRecordId);
                SendRunManifest(refreshRecord);

                var stageExecutionModes = DetermineStageExecutionModes(refreshRecord);

                currentStage = DataRefreshStage.MetadataDictionaryRefresh;
                await RefreshHlaMetadataDictionary(refreshRecord);

                foreach (var dataRefreshStage in orderedRefreshStages.Except(new[] { DataRefreshStage.MetadataDictionaryRefresh }))
                {
                    currentStage = dataRefreshStage;
                    var executionMode = stageExecutionModes[dataRefreshStage];
                    await ExecuteDataRefreshStage(dataRefreshStage, executionMode, refreshRecord);
                }

                return refreshRecord.HlaNomenclatureVersion;
            }
            catch (Exception ex)
            {
                // Surface WHICH stage failed as queryable Exception telemetry. Previously this was only a default-level
                // Trace, which does not populate the App Insights `exceptions` table and is easy to lose in the noise.
                // SqlExceptions are the designed "rethrow -> Service Bus redelivery -> resume from checkpoint" path, so
                // they are logged at Error to avoid crying wolf on every retryable blip; anything else is a genuine
                // terminal failure and is logged Critical. Behaviour (teardown + rethrow) is otherwise unchanged.
                var isRetryableSqlException = ex is SqlException;
                logger.SendException(
                    ex,
                    isRetryableSqlException ? LogLevel.Error : LogLevel.Critical,
                    new Dictionary<string, string>
                    {
                        ["DataRefreshStage"] = currentStage?.ToString() ?? "(before first stage)",
                        ["DataRefreshRecordId"] = refreshRecordId.ToString(),
                        ["Disposition"] = isRetryableSqlException
                            ? "Transient SqlException - will resume from checkpoint on Service Bus redelivery"
                            : "Terminal failure"
                    }
                );
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
        /// <remarks>
        /// <see cref="DataRefreshStage.QueuedDonorUpdateProcessing"/> now also sits between the two scaling stages, but is deliberately excluded
        /// from this test. It can never be skipped, so including it would disable this optimisation outright - and a queue drain on its own is not
        /// worth a full scale-up/scale-down cycle. It still benefits from a database left scaled up, which is why it runs before the scale-down.
        /// </remarks>
        private void AvoidScalingDbUpAndImmediatelyBackDown(Dictionary<DataRefreshStage, DataRefreshStageExecutionMode> modes)
        {
            var stagesRequiringAScaledUpDatabase = orderedRefreshStages.Where(stage =>
                stage > DataRefreshStage.DatabaseScalingSetup
                && stage < DataRefreshStage.DatabaseScalingTearDown
                && stage != DataRefreshStage.QueuedDonorUpdateProcessing
            );
            var areWeSkippingEveryStageBetweenDbScaling = stagesRequiringAScaledUpDatabase.All(stage => modes[stage] == DataRefreshStageExecutionMode.Skip);
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
                // Timed inside the guard, so a continuation - which correctly skips this stage - does not emit a
                // near-zero sample and drag the stage's average down. See the note in ExecuteDataRefreshStage.
                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.StageDurationMsMetric,
                           DataRefreshMetrics.StageDims(nameof(DataRefreshStage.MetadataDictionaryRefresh))))
                {
                    var newHlaNomenclatureVersion =
                        await activeVersionHlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
                    refreshRecord.HlaNomenclatureVersion = newHlaNomenclatureVersion; //Later steps will make use of this value.
                    loggingContext.HlaNomenclatureVersion = newHlaNomenclatureVersion;
                    await dataRefreshHistoryRepository.UpdateExecutionDetails(refreshRecord.Id, newHlaNomenclatureVersion);
                    await dataRefreshHistoryRepository.MarkStageAsComplete(refreshRecord, DataRefreshStage.MetadataDictionaryRefresh);
                }
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
                        LogLevel.Verbose
                    );
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

            // Every stage passes through this one choke point, so one timer here gives all nine stages a real,
            // never-sampled duration. This supersedes deriving stage durations by DATEDIFF-ing the DataRefreshHistory
            // completion columns: those are last-write-wins, so they are only meaningful when the job ran exactly
            // once, and they give the short stages no number at all. The span covers MarkStageAsComplete too, so the
            // two substrates measure the same thing and can be cross-checked against each other.
            //
            // Skipped / not-applicable stages return above without emitting: a zero-length "execution" that never
            // happened would drag every average down and misrepresent a continuation as a fast run.
            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.StageDurationMsMetric,
                       DataRefreshMetrics.StageDims(dataRefreshStage.ToString())))
            {
                await RunDataRefreshStage(dataRefreshStage, executionMode, refreshRecord);
                await dataRefreshHistoryRepository.MarkStageAsComplete(refreshRecord, dataRefreshStage);
            }
        }

        private async Task RunDataRefreshStage(
            DataRefreshStage dataRefreshStage,
            DataRefreshStageExecutionMode executionMode,
            DataRefreshRecord refreshRecord)
        {
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

                    // This deletion must not be removed, and this stage must not gain a mid-stage resume: the importer writes donor management log
                    // entries create-only, which relies on the log table being empty of the donors being imported. In FromScratch mode that is
                    // guaranteed by DataRefreshStage.DataDeletion having just run, or by this stage never having written anything in the interrupted
                    // run it is following. See DonorImporter.InsertDonorBatch.
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
                        isContinuation
                    );
                    break;
                case DataRefreshStage.IndexRecreation:
                    await donorImportRepository.CreateHlaTableIndexes();
                    break;

                case DataRefreshStage.DatabaseScalingTearDown:
                    await ScaleDatabase(
                        dataRefreshSettings.ActiveDatabaseSize.ParseToEnum<AzureDatabaseSize>(),
                        dataRefreshSettings.ActiveDatabaseAutoPauseTimeout
                    );
                    break;
                case DataRefreshStage.QueuedDonorUpdateProcessing:
                    var dbBeingRefreshed = refreshRecord.Database.ParseToEnum<TransientDatabase>();
                    await differentialDonorUpdateProcessor.ApplyDifferentialDonorUpdatesDuringRefresh(dbBeingRefreshed,
                        refreshRecord.HlaNomenclatureVersion
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataRefreshStage), dataRefreshStage, null);
            }
        }

        /// <summary>
        /// Records everything about this run that a later comparison needs and cannot recover afterwards: which
        /// nomenclature, which database, which tiers, which batch geometry, which host. Emitted as an Event rather
        /// than a Trace so it is queryable by field rather than by string-matching a message.
        /// </summary>
        /// <remarks>
        /// The lease owner and a first-class attempt identity belong here too, and arrive with the run-lease work;
        /// until then <see cref="DataRefreshRecord.RefreshAttemptedCount"/> is the only attempt signal there is.
        /// </remarks>
        private void SendRunManifest(DataRefreshRecord refreshRecord)
        {
            logger.SendEvent(RunManifestEventName, LogLevel.Info, new Dictionary<string, string>
            {
                ["DataRefreshRecordId"] = refreshRecord.Id.ToString(),
                ["RefreshAttemptedCount"] = refreshRecord.RefreshAttemptedCount.ToString(),
                ["TargetDatabase"] = refreshRecord.Database,
                ["HlaNomenclatureVersion"] = refreshRecord.HlaNomenclatureVersion ?? "(to be determined this run)",
                ["ShouldMarkAllDonorsAsUpdated"] = refreshRecord.ShouldMarkAllDonorsAsUpdated.ToString(),

                ["DatabaseAName"] = dataRefreshSettings.DatabaseAName,
                ["DatabaseBName"] = dataRefreshSettings.DatabaseBName,
                ["ActiveDatabaseSize"] = dataRefreshSettings.ActiveDatabaseSize,
                ["DormantDatabaseSize"] = dataRefreshSettings.DormantDatabaseSize,
                ["RefreshDatabaseSize"] = dataRefreshSettings.RefreshDatabaseSize,
                ["FullyTransactionalDonorUpdates"] = dataRefreshSettings.DataRefreshDonorUpdatesShouldBeFullyTransactional.ToString(),

                // The EFFECTIVE values, i.e. after the fallback to each historic default - not the raw settings.
                ["DonorImportBatchSize"] = (dataRefreshSettings.DonorImportBatchSize ?? DonorImporter.DefaultBatchSize).ToString(),
                ["HlaProcessingBatchSize"] = (dataRefreshSettings.HlaProcessingBatchSize ?? HlaProcessor.DefaultBatchSize).ToString(),
                ["SqlBulkCopyBatchSize"] =
                    (dataRefreshSettings.SqlBulkCopyBatchSize ?? DataRefreshRepositorySettings.DefaultSqlBulkCopyBatchSize).ToString(),
                ["BatchProgressReportingPeriod"] =
                    (dataRefreshSettings.BatchProgressReportingPeriod ?? HlaProcessor.DefaultBatchProgressReportingPeriod).ToString(),

                // THROWAWAY, ATL-216 H22. Recorded because it is the one thing about this run that makes its stage-40
                // wall clock non-comparable with record 25's: the mgmt-log write rotates through these rows-per-round-
                // trip values, so the stage total is a blend of them. A run whose manifest does not say which ladder it
                // used cannot be reconstructed at a single rung afterwards.
                ["MgmtLogBulkCopyBatchSizeLadder"] = string.Join(",", DonorImporter.MgmtLogBulkCopyBatchSizeLadder),

                ["MachineName"] = Environment.MachineName,
                ["ProcessorCount"] = Environment.ProcessorCount.ToString(),
                ["SiteName"] = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "(not an app service)",
                ["PlanSku"] = Environment.GetEnvironmentVariable("WEBSITE_SKU") ?? "(unknown)",
                ["InstanceId"] = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "(unknown)"
            });
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
                    dataRefreshSettings.DormantDatabaseAutoPauseTimeout
                );
            }
            catch (Exception e)
            {
                logger.SendTrace($"{LoggingPrefix} Teardown failed. Database will need scaling down manually. Exception: {e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendTeardownFailureAlert(recordId);
            }
        }
    }
}