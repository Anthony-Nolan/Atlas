using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;

namespace Atlas.MatchingAlgorithm.Services.DonorManagement
{
    /// <summary>
    /// Manages the addition or removal of individual donors from the search algorithm database.
    /// </summary>
    public interface IDonorManagementService
    {
        Task ApplyDonorUpdatesToDatabase(
            IReadOnlyCollection<DonorAvailabilityUpdate> donorAvailabilityUpdates,
            TransientDatabase targetDatabase,
            string targetHlaNomenclatureVersion,
            bool runAllHlaInsertionsInASingleTransactionScope);
    }

    public class DonorManagementService : IDonorManagementService
    {
        private class NonApplicableUpdate
        {
            public DonorManagementLog DonorManagementLog { get; }
            public DonorAvailabilityUpdate DonorAvailabilityUpdate { get; }

            public NonApplicableUpdate(DonorManagementLog log, DonorAvailabilityUpdate update)
            {
                DonorManagementLog = log;
                DonorAvailabilityUpdate = update;
            }
        }

        private const string TraceMessagePrefix = nameof(ApplyDonorUpdatesToDatabase);

        private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
        private readonly IDonorService donorService;
        private readonly IMatchingAlgorithmImportLogger logger;

        public DonorManagementService(
            IStaticallyChosenDatabaseRepositoryFactory repositoryFactory,
            IDonorService donorService,
            IMatchingAlgorithmImportLogger logger)
        {
            this.repositoryFactory = repositoryFactory;
            this.donorService = donorService;
            this.logger = logger;
        }

        public async Task ApplyDonorUpdatesToDatabase(
            IReadOnlyCollection<DonorAvailabilityUpdate> donorAvailabilityUpdates,
            TransientDatabase targetDatabase,
            string targetHlaNomenclatureVersion,
            bool runAllHlaInsertionsInASingleTransactionScope)
        {
            var filteredUpdates = await FilterUpdates(donorAvailabilityUpdates, targetDatabase);
            await ApplyDonorUpdates(filteredUpdates, targetDatabase, targetHlaNomenclatureVersion, runAllHlaInsertionsInASingleTransactionScope);
        }

        private async Task<List<DonorAvailabilityUpdate>> FilterUpdates(IReadOnlyCollection<DonorAvailabilityUpdate> updates, TransientDatabase targetDatabase)
        {
            //TODO: ATLAS-95 Consider alternate approach:
            // Run through list of updates, building a dict of updates to apply, keyed by Id.
            // loop TryAdd.
            //  If record exists: compare and either replace the record, or don't
            // Then read DonorId + Date from DB for Ids in dict.Keys.
            // Loop through results and remove record if dates are wrong way around.
            // At the end you'll be left with the dictionary of "updates that should be applied", keyed by DonorId.

            var applicableUpdate = RetainLatestUpdateInBatchPerDonorId(updates);
            return await RetainUpdatesThatAreNewerThanAnyPreviouslyAppliedUpdate(applicableUpdate, targetDatabase);
        }

        /// <remarks>
        /// Equivalently "Filter out updates that are already superseded within this batch".
        /// </remarks>
        private static IEnumerable<DonorAvailabilityUpdate> RetainLatestUpdateInBatchPerDonorId(IReadOnlyCollection<DonorAvailabilityUpdate> updates)
        {
            return updates
                .GroupBy(u => u.DonorId)
                .Select(grp => grp.OrderByDescending(u => u.UpdateDateTime).First());
        }

        /// <summary>
        /// Examines the Database, and compares the records of previously applied updates for the donor, versus the update that we're considering applying to the donor.
        /// If the DB record is newer (i.e. the update we've been given is already obsolete), then does NOT return it. 
        /// </summary>
        /// <returns>
        /// Those updates that are newer than the last update recorded in the donor
        /// management log, or those where the donor has no record of a previous update.
        /// </returns>
        private async Task<List<DonorAvailabilityUpdate>> RetainUpdatesThatAreNewerThanAnyPreviouslyAppliedUpdate(IEnumerable<DonorAvailabilityUpdate> updates, TransientDatabase targetDatabase)
        {
            var allUpdates = updates.ToList();

            var logRepository = repositoryFactory.GetDonorManagementLogRepositoryForDatabase(targetDatabase);
            var existingLogs = await logRepository.GetDonorManagementLogBatch(allUpdates.Select(u => u.DonorId));

            // TODO: ATLAS-95 benchmark this approach, vs. pulling DonorId + last update date out of DB and doing join in memory.
            // GroupJoin is equivalent to a LEFT OUTER JOIN
            var updatesWithLogs = allUpdates
                .GroupJoin(existingLogs,
                    update => update.DonorId,
                    log => log.DonorId,
                    (update, logs) => new { Update = update, Log = logs.SingleOrDefault() })
                .Select(a => new
                {
                    a.Update,
                    a.Log,
                    IsApplicable = a.Log == null || a.Update.UpdateDateTime > a.Log.LastUpdateDateTime
                })
                .ToList();

            var (applicableUpdates, nonApplicableUpdates) = updatesWithLogs.ReifyAndSplit(u => u.IsApplicable);

            LogNonApplicableUpdates(nonApplicableUpdates.Select(u => new NonApplicableUpdate(u.Log, u.Update)));

            return applicableUpdates.Select(u => u.Update).ToList();
        }

        private void LogNonApplicableUpdates(IEnumerable<NonApplicableUpdate> nonApplicableUpdates)
        {
            nonApplicableUpdates = nonApplicableUpdates.ToList();

            if (!nonApplicableUpdates.Any())
            {
                return;
            }

            foreach (var update in nonApplicableUpdates)
            {
                logger.SendEvent(GetDonorUpdateNotAppliedEventModel(update));
            }

            logger.SendTrace(
                $"{TraceMessagePrefix}: {nonApplicableUpdates.Count()} donor updates were not applied " +
                "due to being older than previously applied updates (AI event logged for each update).",
                LogLevel.Warn);
        }

        private static DonorUpdateNotAppliedEventModel GetDonorUpdateNotAppliedEventModel(NonApplicableUpdate update)
        {
            return new DonorUpdateNotAppliedEventModel(update.DonorManagementLog.LastUpdateDateTime, update.DonorAvailabilityUpdate);
        }

        private async Task ApplyDonorUpdates(
            List<DonorAvailabilityUpdate> updatesList,
            TransientDatabase targetDatabase,
            string targetHlaNomenclatureVersion,
            bool runAllHlaInsertionsInASingleTransactionScope
            )
        {
            logger.SendTrace($"{TraceMessagePrefix}: {updatesList.Count} donor updates to be applied.");

            var (availableUpdates, unavailableUpdates) = updatesList.ReifyAndSplit(upd => upd.IsAvailableForSearch);
            var recordOfUpdatesApplied = updatesList.Select(upd => new DonorManagementInfo{ DonorId = upd.DonorId, UpdateDateTime = upd.UpdateDateTime, UpdateSequenceNumber = upd.UpdateSequenceNumber}).ToList();

            // Note that as of .NET Core 3, TransactionScope does not support Distributed Transactions: https://github.com/dotnet/runtime/issues/715
            // It's slated for .NET Core 5, which is some way off.
            // TransactionScope will (attempt to) escalate to a DT as soon as it detects 2 simultaneously open Connections.
            // Having multiple *sequential* connections is fine - it's only running in parallel that breaks.
            // Accordingly the entire code path below this point has to run its connections in series not in parallel.
            // This is a shame as it causes a ~30% performance hit, from not being able to write the different matching PGroup tables in parallel.
            // Any attempt to use a single connection across multiple threads will also fail, as MARS allows queries to be "run"
            // in parallel, but not to *EXECUTE* in parallel - they get interleaved, so we lose all the benefit.
            // Hopefully DT support will comeback sooner rather than later, and we can re-parallelize the per-Loci matching PGroup writing.
            using (var transactionScope = new OptionalAsyncTransactionScope(runAllHlaInsertionsInASingleTransactionScope))
            {
                await AddOrUpdateDonors(availableUpdates, targetDatabase, targetHlaNomenclatureVersion, runAllHlaInsertionsInASingleTransactionScope);
                await SetDonorsAsUnavailableForSearch(unavailableUpdates, targetDatabase);
                await CreateOrUpdateManagementLogBatch(recordOfUpdatesApplied, targetDatabase);
                transactionScope.Complete();
            }
        }

        private async Task AddOrUpdateDonors(
            List<DonorAvailabilityUpdate> availableUpdates,
            TransientDatabase targetDatabase,
            string targetHlaNomenclatureVersion,
            bool runAllHlaInsertionsInASingleTransactionScope)
        {
            var (updatesWithoutInfo, updatesWithInfo) = availableUpdates.ReifyAndSplit(upd => upd.DonorInfo == null);

            if (updatesWithoutInfo.Any())
            {
                var donorIds = updatesWithoutInfo.Select(upd => upd.DonorId.ToString()).StringJoin(", ");
                throw new InvalidOperationException(
@"An available Donor update was provided with no DonorInfo.
This should never happen, and suggests high-level flaws in the DonorUpdate process.
No donors in this message batch have been imported.
Invalid DonorIds: " + donorIds);
            }

            var availableDonors = updatesWithInfo.Select(d => d.DonorInfo).ToList();

            if (availableDonors.Any())
            {
                logger.SendTrace($"{TraceMessagePrefix}: {availableDonors.Count} donors to be added or updated.");

                await donorService.CreateOrUpdateDonorBatch(availableDonors, targetDatabase, targetHlaNomenclatureVersion, runAllHlaInsertionsInASingleTransactionScope);
            }
        }

        private async Task SetDonorsAsUnavailableForSearch(List<DonorAvailabilityUpdate> unavailableUpdates, TransientDatabase targetDatabase)
        {
            if (unavailableUpdates.Any())
            {
                logger.SendTrace($"{TraceMessagePrefix}: {unavailableUpdates.Count} donors to be marked as unavailable for search.");

                var unavailableDonorIds = unavailableUpdates.Select(d => d.DonorId).ToList();
                await donorService.SetDonorBatchAsUnavailableForSearch(unavailableDonorIds, targetDatabase);
            }
        }

        private async Task CreateOrUpdateManagementLogBatch(List<DonorManagementInfo> appliedUpdates, TransientDatabase targetDatabase)
        {
            if (!appliedUpdates.Any())
            {
                return;
            }

            var logRepository = repositoryFactory.GetDonorManagementLogRepositoryForDatabase(targetDatabase);
            await logRepository.CreateOrUpdateDonorManagementLogBatch(appliedUpdates);
        }
    }
}
