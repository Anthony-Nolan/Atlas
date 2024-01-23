using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Mapping;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IDonorImportRepository = Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates.IDonorImportRepository;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport
{
    /// <summary>
    /// Responsible for fetching all eligible donors for the search algorithm.
    /// Only responsible for one off import of all donors into the matching algorithm's data store. For individual updates, <see cref="IDonorUpdateProcessor"/>
    /// </summary>
    public interface IDonorImporter
    {
        /// <summary>
        /// Fetches all donors and stores their data in the donor table
        /// Does not perform analysis of donor p-groups
        /// </summary>
        /// <param name="shouldMarkDonorsAsUpdated">
        /// When set, all donors will have corresponding entries added to the donor management log table.
        /// </param>
        Task ImportDonors(bool shouldMarkDonorsAsUpdated = false);
    }

    public class DonorImporter : IDonorImporter
    {
        private const int BatchSize = 10000;
        private const string ImportFailureEventName = "Donor Import Failure(s) in the Matching Algorithm's DataRefresh";

        private readonly IDonorImportRepository matchingDonorImportRepository;
        private readonly IDonorManagementLogRepository donorManagementLogRepository;
        private readonly IDonorInfoConverter donorInfoConverter;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly IDonorReader donorReader;

        public DonorImporter(
            IDormantRepositoryFactory repositoryFactory,
            IDonorInfoConverter donorInfoConverter,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IMatchingAlgorithmImportLogger logger,
            IDonorReader donorReader)
        {
            matchingDonorImportRepository = repositoryFactory.GetDonorImportRepository();
            donorManagementLogRepository = repositoryFactory.GetDonorManagementLogRepository();
            this.donorInfoConverter = donorInfoConverter;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.logger = logger;
            this.donorReader = donorReader;
        }

        public async Task ImportDonors(bool shouldMarkDonorsAsUpdated)
        {
            try
            {
                var allFailedDonors = new List<FailedDonorInfo>();
                var donorsStream = donorReader.StreamAllDonors().Select(d => d.MapImportDonorToMatchingUpdateDonor());
                foreach (var streamedDonorBatch in donorsStream.Batch(BatchSize))
                {
                    var reifiedDonorBatch = streamedDonorBatch.ToList();
                    var failedDonors = await InsertDonorBatch(reifiedDonorBatch, shouldMarkDonorsAsUpdated, DateTimeOffset.UtcNow);
                    allFailedDonors.AddRange(failedDonors);
                }

                await failedDonorsNotificationSender.SendFailedDonorsAlert(allFailedDonors, ImportFailureEventName, Priority.Medium);
                logger.SendTrace("Donor import is complete");
            }
            catch (Exception ex)
            {
                logger.SendTrace($"Donor Import Failed: {ex.Message}", LogLevel.Error);
                throw new DonorImportHttpException("Unable to complete donor import: " + ex.Message, ex);
            }
        }

        /// <param name="donors">Batch of donors to insert into the matching database.</param>
        /// <param name="shouldMarkDonorsAsUpdated"></param>
        /// <param name="batchFetchTime">
        ///     Time at which this batch were fetched from the master donor store, to be used as the "last updated" time of these donors.
        ///     It is slightly more correct to use the fetch time than the insert time, in the case of a race condition where a new update is published between
        ///     fetching a batch from the donor store, and inserting it into the donor management log table.
        /// </param>
        /// <returns>Details of donors in the batch that failed import</returns>
        private async Task<IEnumerable<FailedDonorInfo>> InsertDonorBatch(
            List<SearchableDonorInformation> donors,
            bool shouldMarkDonorsAsUpdated,
            DateTimeOffset batchFetchTime)
        {
            using (logger.RunTimed($"Import donor batch (BatchSize: {BatchSize})", LogLevel.Verbose))
            {
                var donorInfoConversionResult = await donorInfoConverter.ConvertDonorInfoAsync(donors, ImportFailureEventName);
                await matchingDonorImportRepository.InsertBatchOfDonors(donorInfoConversionResult.ProcessingResults);

                if (shouldMarkDonorsAsUpdated)
                {
                    await donorManagementLogRepository.CreateOrUpdateDonorManagementLogBatch(donors.Select(d => new DonorManagementInfo
                        {
                            DonorId = d.DonorId,
                            UpdateDateTime = batchFetchTime,
                            // This assumes that all updates come from a service bus message, which is incorrect for the initial donor import
                            // TODO: ATLAS-972: Confirm this is unused and remove
                            UpdateSequenceNumber = -1
                        }
                    ));
                }

                return donorInfoConversionResult.FailedDonors;
            }
        }
    }
}