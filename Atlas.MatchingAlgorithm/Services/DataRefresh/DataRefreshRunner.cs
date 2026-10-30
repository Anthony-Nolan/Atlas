using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
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
        /// - Precomputes the genotype sets of all donors, unless <see cref="DonorGenotypePrecomputationSwitchSettings.Enabled"/> is false
        /// - Scales down target database
        /// </summary>
        /// <remarks>
        /// <para>
        /// A pending manual decision on the record is applied first: <see cref="DataRefreshManualDecision.Complete"/>
        /// marks the donor genotype precomputation stage as complete, and
        /// <see cref="DataRefreshManualDecision.RetryFailedBatches"/> is left for that stage to apply.
        /// </para>
        ///
        /// <para>
        /// When more donors failed in the donor genotype precomputation stage than the threshold allows, the refresh pauses:
        /// it starts the scale-down of the dormant database, sends the paused alert, and waits until the scale-down ends.
        /// The later stages do not run, and this method does not write the record: the caller sets the pause.
        /// </para>
        ///
        /// <para>
        /// When all stages are complete, the staging data of the donor genotype precomputation stage is removed, before the
        /// caller closes the record. Normally the stage removed it already. After a manual Complete decision, it is still there.
        /// </para>
        /// </remarks>
        /// <param name="cancellationToken">
        /// Cancelled if this invocation loses its run-level lease on the refresh record, meaning another invocation has
        /// taken the record over. Observed at batch boundaries, so cancellation bounds the damage a displaced invocation
        /// can do to one batch rather than stopping it instantly.
        /// </param>
        /// <returns>
        /// Completed, with the version of the HLA Nomenclature used for the new data; or paused, with the failures of the
        /// donor genotype precomputation run.
        /// </returns>
        Task<DataRefreshRunResult> RefreshData(int refreshRecordId, CancellationToken cancellationToken = default);
    }

    public class DataRefreshRunner : IDataRefreshRunner
    {
        private readonly DataRefreshSettings dataRefreshSettings;
        private readonly DonorGenotypePrecomputationSwitchSettings precomputationSwitchSettings;
        private readonly DonorGenotypePrecomputationSettings precomputationSettings;
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
        private readonly IDonorGenotypePrecomputationStage donorGenotypePrecomputationStage;
        private readonly IDonorGenotypePrecomputationRunCanceller precomputationRunCanceller;
        private readonly IDonorUpdateProcessor differentialDonorUpdateProcessor;
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly IDataRefreshManualDecisionEvents manualDecisionEvents;

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
            // Note: DonorImport writes donor management log entries create-only, on the assumption that this stage has emptied that table
            // beforehand. Skipping this stage is only safe because DonorImport is itself either skipped, or restarted from scratch with its own
            // deletion, whenever this stage is skipped. See the DonorImport case in ExecuteDataRefreshStage.
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

            // When this stage is complete, its run is complete, so nothing is left to do. Its staging data is gone, except after a
            // manual Complete decision: then it is removed when all stages are complete.
            {DataRefreshStage.DonorGenotypePrecomputation, true},

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
            IDonorGenotypePrecomputationStage donorGenotypePrecomputationStage,
            IDonorGenotypePrecomputationRunCanceller precomputationRunCanceller,
            IDonorUpdateProcessor differentialDonorUpdateProcessor,
            IMatchingAlgorithmImportLogger logger,
            IDataRefreshSupportNotificationSender dataRefreshNotificationSender,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            MatchingAlgorithmImportLoggingContext loggingContext,
            IDataRefreshManualDecisionEvents manualDecisionEvents,
            DonorGenotypePrecomputationSwitchSettings precomputationSwitchSettings,
            DonorGenotypePrecomputationSettings precomputationSettings)
        {
            this.precomputationSettings = precomputationSettings;
            this.manualDecisionEvents = manualDecisionEvents;
            this.precomputationSwitchSettings = precomputationSwitchSettings;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            this.donorGenotypePrecomputationStage = donorGenotypePrecomputationStage;
            this.precomputationRunCanceller = precomputationRunCanceller;
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
            activeHlaNomenclatureVersion = hlaVersionOrDefault;
        }

        /// <summary>ATL-491 experiment only: the version that a <c>JustPrecompute</c> refresh uses.</summary>
        private readonly string activeHlaNomenclatureVersion;

        public async Task<DataRefreshRunResult> RefreshData(int refreshRecordId, CancellationToken cancellationToken)
        {
            try
            {
                var refreshRecord = await dataRefreshHistoryRepository.GetRecord(refreshRecordId);

                // Before the stage modes: a Complete decision marks a stage as complete, and so changes the modes.
                await ApplyPendingManualDecision(refreshRecord);
                var stageExecutionModes = DetermineStageExecutionModes(refreshRecord);

                await RefreshHlaMetadataDictionary(refreshRecord);

                foreach (var dataRefreshStage in orderedRefreshStages.Except(new[] {DataRefreshStage.MetadataDictionaryRefresh}))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var executionMode = stageExecutionModes[dataRefreshStage];
                    var pauseReport = await ExecuteDataRefreshStage(dataRefreshStage, executionMode, refreshRecord, cancellationToken);
                    if (pauseReport != null)
                    {
                        await PauseForManualDecision(refreshRecord, pauseReport, cancellationToken);
                        return DataRefreshRunResult.Paused(pauseReport);
                    }
                }

                // After a manual Complete decision, the staging data of the precomputation stage is still in the database. The
                // decision is cleared when it is applied, so an attempt after one that stopped in a later stage cannot see it:
                // every attempt that completes removes the data. Before the swap, so that the active database is not touched.
                await precomputationRunCanceller.RemoveStagingData(refreshRecord);

                return DataRefreshRunResult.Completed(refreshRecord.HlaNomenclatureVersion);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Deliberately no teardown. The run was aborted because the lease was lost, so another invocation now
                // owns this record - and the dormant database it is refreshing. Scaling that database back down
                // underneath it would be actively harmful; the invocation that now holds the lease is responsible for
                // its own teardown.
                logger.SendTrace($"{LoggingPrefix} Refresh aborted: the lease on record {refreshRecordId} was lost.", LogLevel.Critical);
                throw;
            }
            catch (Exception ex)
            {
                logger.SendTrace($"{LoggingPrefix} Refresh failed. Exception: {ex}");
                await FailureTearDown(refreshRecordId);
                throw;
            }
        }

        /// <summary>
        /// Applies a manual decision that was taken while the record was paused, and that no attempt has applied yet.
        /// </summary>
        /// <remarks>
        /// <see cref="DataRefreshManualDecision.Complete"/>: the donor genotype precomputation stage is marked as complete,
        /// and the decision is cleared, in one save. So the stages up to that stage are skipped, also the scale-up.
        /// <see cref="DataRefreshManualDecision.RetryFailedBatches"/>: nothing here. The stage applies it, after the
        /// database is scaled up.
        /// </remarks>
        private async Task ApplyPendingManualDecision(DataRefreshRecord refreshRecord)
        {
            if (refreshRecord.PendingManualDecision is not { } decision)
            {
                return;
            }

            manualDecisionEvents.Resumed(refreshRecord.Id, decision);

            if (refreshRecord.JustPrecompute)
            {
                await ApplyExperimentDecision(refreshRecord, decision);
                return;
            }

            switch (decision)
            {
                case DataRefreshManualDecision.Complete:
                    await dataRefreshHistoryRepository.ApplyManualCompleteDecision(refreshRecord);
                    logger.SendTrace(
                        $"{LoggingPrefix} Record {refreshRecord.Id} resumes after the manual decision {decision}: stage " +
                        $"{DataRefreshStage.DonorGenotypePrecomputation} is marked as complete, with its failures. The later stages run.",
                        LogLevel.Warn);
                    break;
                case DataRefreshManualDecision.RetryFailedBatches:
                    logger.SendTrace(
                        $"{LoggingPrefix} Record {refreshRecord.Id} resumes after the manual decision {decision}: the database is scaled " +
                        $"up, and stage {DataRefreshStage.DonorGenotypePrecomputation} computes the failed batches again.",
                        LogLevel.Warn);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Record {refreshRecord.Id} has the pending manual decision {decision}, which a refresh cannot apply.");
            }
        }

        /// <summary>
        /// ATL-491 experiment only. <see cref="DataRefreshManualDecision.RetryFailedBatches"/> runs the donor genotype
        /// precomputation stage again from the start: the precomputed data is removed first, and the decision cleared after
        /// it, so an attempt that stops between the two removes the data again. No other decision resumes such a refresh.
        /// </summary>
        private async Task ApplyExperimentDecision(DataRefreshRecord refreshRecord, DataRefreshManualDecision decision)
        {
            if (decision != DataRefreshManualDecision.RetryFailedBatches)
            {
                throw new InvalidOperationException(
                    $"Data refresh record {refreshRecord.Id} is a JustPrecompute refresh, and cannot resume with the manual decision " +
                    $"{decision}. Only {DataRefreshManualDecision.RetryFailedBatches} (run the stage again) and " +
                    $"{DataRefreshManualDecision.Fail} (end the refresh) are allowed.");
            }

            await donorImportRepository.RemoveAllPrecomputedGenotypeSets(precomputationSettings.JustPrecomputeBucketCount);
            await dataRefreshHistoryRepository.ClearPendingManualDecision(refreshRecord);
            logger.SendTrace(
                $"{LoggingPrefix} ATL-491 experiment: record {refreshRecord.Id} resumes after the manual decision {decision}: the " +
                $"precomputed data is removed, and stage {DataRefreshStage.DonorGenotypePrecomputation} runs again from the start.",
                LogLevel.Warn);
        }

        /// <summary>
        /// Pauses the refresh for a manual decision: starts the scale-down of the dormant database, sends the paused alert,
        /// and waits until the scale-down ends. Does not write the record: the orchestrator sets the pause, in the update
        /// that releases the lease.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The alert goes out before the wait, so support can look at the cause while the database scales down.
        /// </para>
        ///
        /// <para>
        /// The lease is held until the scale-down has ended, and a decision is accepted only when the record is paused. So
        /// a decision cannot start a scale operation of the resumed refresh while this one runs: Azure refuses a second
        /// scale operation on a database.
        /// </para>
        ///
        /// <para>
        /// A scale-down that Azure refuses, or that fails, sends the teardown failure alert, and the pause continues: the
        /// failures still need a decision.
        /// </para>
        /// </remarks>
        private async Task PauseForManualDecision(
            DataRefreshRecord refreshRecord,
            DonorGenotypePrecomputationFailureReport pauseReport,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.SendTrace(
                $"{LoggingPrefix} Record {refreshRecord.Id} pauses for a manual decision: {pauseReport.FailureSummary.FailedDonorCount} of " +
                $"{pauseReport.FailureSummary.TotalDonorCount} donors failed in donor genotype precomputation run {pauseReport.RunId}. The " +
                "dormant database is scaled down.",
                LogLevel.Warn);

            var scaleDown = await TryStartDormantScaleDown(refreshRecord.Id);
            await dataRefreshNotificationSender.SendPrecomputationPausedAlert(refreshRecord.Id, pauseReport, scaleDown != null);

            if (scaleDown != null)
            {
                await TryWaitForDormantScaleDown(refreshRecord.Id, scaleDown);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <returns>The operation, or null when Azure refused it: the teardown failure alert is sent.</returns>
        private async Task<DatabaseScalingOperation> TryStartDormantScaleDown(int recordId)
        {
            try
            {
                return await azureDatabaseManager.StartDatabaseScaling(
                    DormantDatabaseName(),
                    dataRefreshSettings.DormantDatabaseSize.ParseToEnum<AzureDatabaseSize>(),
                    dataRefreshSettings.DormantDatabaseAutoPauseTimeout);
            }
            catch (Exception e)
            {
                await ReportTeardownFailure(recordId, e);
                return null;
            }
        }

        private async Task TryWaitForDormantScaleDown(int recordId, DatabaseScalingOperation scaleDown)
        {
            try
            {
                await azureDatabaseManager.WaitForDatabaseScaling(scaleDown);
            }
            catch (Exception e)
            {
                await ReportTeardownFailure(recordId, e);
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

            if (refreshRecord.JustPrecompute)
            {
                SkipAllButThePrecomputation(modes);
            }

            return modes;
        }

        /// <summary>
        /// ATL-491 experiment only. A <c>JustPrecompute</c> refresh keeps the donors and the HLA of the dormant database, and its
        /// indexes. <see cref="DataRefreshStage.DataDeletion"/> removes only the precomputed data, once per record. The stage
        /// always pauses when its run is complete, so the stages after it never run.
        /// </summary>
        private static void SkipAllButThePrecomputation(Dictionary<DataRefreshStage, DataRefreshStageExecutionMode> modes)
        {
            modes[DataRefreshStage.IndexRemoval] = DataRefreshStageExecutionMode.Skip;
            modes[DataRefreshStage.DonorImport] = DataRefreshStageExecutionMode.Skip;
            modes[DataRefreshStage.DonorHlaProcessing] = DataRefreshStageExecutionMode.Skip;
            modes[DataRefreshStage.IndexRecreation] = DataRefreshStageExecutionMode.Skip;
            modes[DataRefreshStage.DatabaseScalingSetup] = DataRefreshStageExecutionMode.FromScratch;
            if (modes[DataRefreshStage.DonorGenotypePrecomputation] == DataRefreshStageExecutionMode.Skip)
            {
                modes[DataRefreshStage.DonorGenotypePrecomputation] = DataRefreshStageExecutionMode.FromScratch;
            }
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
        /// <remarks>
        /// A run has exactly one HLA Nomenclature version and one HLA Metadata Dictionary snapshot, however many attempts it takes:
        /// - The version is read from WMDA only once, and recorded BEFORE the dictionary is recreated. An attempt that stops
        ///   during recreation therefore leaves the version behind, and the next attempt recreates under that same version,
        ///   even if WMDA has published a newer one since.
        /// - The snapshot is recorded together with the stage completion. Until then, every attempt recreates the dictionary,
        ///   and the snapshot of the last recreation is the one recorded. Once recorded, no later attempt recreates it again.
        /// </remarks>
        private async Task RefreshHlaMetadataDictionary(DataRefreshRecord refreshRecord)
        {
            if (string.IsNullOrEmpty(refreshRecord.HlaNomenclatureVersion))
            {
                var latestHlaNomenclatureVersion = refreshRecord.JustPrecompute
                    ? activeHlaNomenclatureVersion
                    : activeVersionHlaMetadataDictionary.GetLatestStableHlaNomenclatureVersion();
                refreshRecord.HlaNomenclatureVersion = latestHlaNomenclatureVersion; //Later steps will make use of this value.
                await dataRefreshHistoryRepository.UpdateExecutionDetails(refreshRecord.Id, latestHlaNomenclatureVersion);
            }

            loggingContext.HlaNomenclatureVersion = refreshRecord.HlaNomenclatureVersion;

            if (refreshRecord.JustPrecompute)
            {
                // ATL-491 experiment only: the dormant database keeps its donors and their HLA, so the run uses the active
                // version and dictionary as they are.
                return;
            }

            if (refreshRecord.IsStageComplete(DataRefreshStage.MetadataDictionaryRefresh))
            {
                // We are continuing a run that already recreated the dictionary. We MUST continue with that same dictionary.
                return;
            }

            // Always rebuild, even when the run's version is already the active one:
            // a forced refresh on an unchanged version must still repair the dictionary.
            var recreation = await activeVersionHlaMetadataDictionary.RecreateHlaMetadataDictionary(
                CreationBehaviour.Specific(refreshRecord.HlaNomenclatureVersion));

            var snapshotUtc = recreation.SnapshotUtc ?? throw new InvalidOperationException(
                $"HLA Metadata Dictionary was not recreated at HLA Nomenclature version {refreshRecord.HlaNomenclatureVersion}, " +
                "so there is no snapshot to record.");

            logger.SendTrace($"{LoggingPrefix} HLA Metadata Dictionary recreated at HLA Nomenclature version " +
                             $"{refreshRecord.HlaNomenclatureVersion}, snapshot {snapshotUtc:O}.");
            await dataRefreshHistoryRepository.MarkHlaMetadataDictionaryRefreshAsComplete(refreshRecord, snapshotUtc);
        }

        /// <returns>
        /// Null when the stage is complete. The failures of the donor genotype precomputation run when that stage is not
        /// complete, because the refresh must pause for a manual decision.
        /// </returns>
        private async Task<DonorGenotypePrecomputationFailureReport> ExecuteDataRefreshStage(
            DataRefreshStage dataRefreshStage,
            DataRefreshStageExecutionMode executionMode,
            DataRefreshRecord refreshRecord,
            CancellationToken cancellationToken)
        {
            switch (executionMode)
            {
                //Note the distinction between `break`s and `return`s here!
                case DataRefreshStageExecutionMode.NotApplicable:
                    logger.SendTrace($"{LoggingPrefix} Stage {dataRefreshStage} is not Applicable to the 'All Stages' execution loop.",
                        LogLevel.Verbose);
                    return null;
                case DataRefreshStageExecutionMode.Skip:
                    logger.SendTrace($"{LoggingPrefix} Stage {dataRefreshStage} is already complete and can be skipped. Skipping.");
                    return null;
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
                    if (refreshRecord.JustPrecompute)
                    {
                        await donorImportRepository.RemoveAllPrecomputedGenotypeSets(precomputationSettings.JustPrecomputeBucketCount);
                        break;
                    }

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
                    await donorImporter.ImportDonors(refreshRecord.ShouldMarkAllDonorsAsUpdated, cancellationToken);
                    break;
                case DataRefreshStage.DonorHlaProcessing:
                    var isContinuation = (executionMode == DataRefreshStageExecutionMode.Continuation);
                    var verbPhrase = isContinuation ? "Continuing existing processing of" : "Beginning processing of";
                    logger.SendTrace($"{LoggingPrefix} {verbPhrase} Donors using HLA Nomenclature version: {refreshRecord.HlaNomenclatureVersion}");
                    await hlaProcessor.UpdateDonorHla(
                        refreshRecord.HlaNomenclatureVersion,
                        donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecord.Id, donorId),
                        refreshRecord.LastSafelyProcessedDonor,
                        isContinuation,
                        cancellationToken);
                    break;
                case DataRefreshStage.IndexRecreation:
                    await donorImportRepository.CreateHlaTableIndexes();
                    break;
                case DataRefreshStage.DonorGenotypePrecomputation:
                    if (!precomputationSwitchSettings.Enabled)
                    {
                        // Marked as complete, so the later stages and the timers of the stage treat it as done. The genotype
                        // sets of the database were removed with its donor data, so search imputes every donor live.
                        logger.SendTrace(
                            $"{LoggingPrefix} Stage {dataRefreshStage} is turned off (DonorGenotypePrecomputation:Enabled is false). " +
                            "No genotype sets are precomputed. The stage is marked as complete.");
                        break;
                    }

                    var precomputationResult = await donorGenotypePrecomputationStage.Run(refreshRecord, executionMode, cancellationToken);
                    if (precomputationResult.IsAwaitingManualDecision)
                    {
                        // Not marked as complete: the decision decides how the stage ends.
                        return precomputationResult.FailureReport;
                    }

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
            return null;
        }

        private async Task ScaleDatabase(AzureDatabaseSize targetSize, int? autoPauseDuration = null)
        {
            await azureDatabaseManager.UpdateDatabaseSize(DormantDatabaseName(), targetSize, autoPauseDuration);
        }

        private string DormantDatabaseName() => azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());

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
                await ReportTeardownFailure(recordId, e);
            }
        }

        /// <summary>The dormant database could not be scaled down to the dormant size. Logs, and sends the alert.</summary>
        private async Task ReportTeardownFailure(int recordId, Exception exception)
        {
            logger.SendTrace($"{LoggingPrefix} Teardown failed. Database will need scaling down manually. Exception: {exception}", LogLevel.Critical);
            await dataRefreshNotificationSender.SendTeardownFailureAlert(recordId);
        }
    }
}