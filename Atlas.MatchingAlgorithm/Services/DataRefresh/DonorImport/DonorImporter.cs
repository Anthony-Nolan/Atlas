using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Mapping;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using MoreLinq;

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
        Task ImportDonors();
    }

    public class DonorImporter : IDonorImporter
    {
        private const int BatchSize = 10000;
        private const string ImportFailureEventName = "Donor Import Failure(s) in the Matching Algorithm's DataRefresh";

        private readonly IDonorImportRepository matchingDonorImportRepository;
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
            this.donorInfoConverter = donorInfoConverter;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.logger = logger;
            this.donorReader = donorReader;
        }

        public async Task ImportDonors()
        {
            try
            {
                var allFailedDonors = new List<FailedDonorInfo>();
                var donorsStream = donorReader.StreamAllDonors().Select(d => d.MapImportDonorToMatchingUpdateDonor());
                foreach (var streamedDonorBatch in donorsStream.Batch(BatchSize))
                {
                    var reifiedDonorBatch = streamedDonorBatch.ToList();
                    var failedDonors = await InsertDonorBatch(reifiedDonorBatch);
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


        /// <returns>Details of donors in the batch that failed import</returns>
        private async Task<IEnumerable<FailedDonorInfo>> InsertDonorBatch(List<SearchableDonorInformation> donors)
        {
            using (logger.RunTimed($"Import donor batch (BatchSize: {BatchSize})", LogLevel.Verbose))
            {
                var donorInfoConversionResult = await donorInfoConverter.ConvertDonorInfoAsync(donors, ImportFailureEventName);
                await matchingDonorImportRepository.InsertBatchOfDonors(donorInfoConversionResult.ProcessingResults);
                return donorInfoConversionResult.FailedDonors;
            }
        }
    }
}