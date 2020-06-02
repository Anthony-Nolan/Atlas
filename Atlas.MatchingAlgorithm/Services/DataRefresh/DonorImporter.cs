using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using MoreLinq;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    /// <summary>
    /// Responsible for fetching all eligible donors for the search algorithm
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
        private const int BatchSize = 100;
        private const string ImportFailureEventName = "Donor Import Failure(s) in the Search Algorithm";

        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorInfoConverter donorInfoConverter;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly ILogger logger;
        private readonly IDonorReader donorReader;

        public DonorImporter(
            IDormantRepositoryFactory repositoryFactory,
            IDonorInfoConverter donorInfoConverter,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            ILogger logger,
            IDonorReader donorReader)
        {
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
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
                var donors = donorReader.GetAllDonors().Select(MapDonor);
                foreach (var donorBatch in donors.Batch(BatchSize))
                {
                    var failedDonors = await InsertDonorBatch(donorBatch);
                    allFailedDonors.AddRange(failedDonors);
                }
                await failedDonorsNotificationSender.SendFailedDonorsAlert(allFailedDonors, ImportFailureEventName, Priority.Medium);
                logger.SendTrace("Donor import is complete", LogLevel.Info);
            }
            catch (Exception ex)
            {
                logger.SendTrace($"Donor Import Failed: {ex.Message}", LogLevel.Error);
                throw new DonorImportHttpException("Unable to complete donor import: " + ex.Message, ex);
            }
        }

        // TODO: QQ Move out of this file to appropriate location?
        private static SearchableDonorInformation MapDonor(Donor donor)
        {
            return new SearchableDonorInformation
            {
                // TODO: ATLAS-294: Do not do this, no guarantee this will be parsable to an int
                DonorId = int.Parse(donor.DonorId),
                // TODO: ATLAS-294: Use enum here, don't parse to and from string when the types otherwise match!
                DonorType = donor.DonorType.ToString(),
                A_1 = donor.A_1,
                A_2 = donor.A_2,
                B_1 = donor.B_1,
                B_2 = donor.B_2,
                C_1 = donor.C_1,
                C_2 = donor.C_2,
                DPB1_1 = donor.DPB1_1,
                DPB1_2 = donor.DPB1_2,
                DQB1_2 = donor.DQB1_1,
                DQB1_1 = donor.DQB1_2,
                DRB1_1 = donor.DRB1_1,
                DRB1_2 = donor.DRB1_2,
            };
        }

        /// <returns>Details of donors in the batch that failed import</returns>
        private async Task<IEnumerable<FailedDonorInfo>> InsertDonorBatch(IEnumerable<SearchableDonorInformation> donors)
        {
            return await TimingLogger.RunTimedAsync(async () =>
                {
                    var donorInfoConversionResult = await donorInfoConverter.ConvertDonorInfoAsync(donors, ImportFailureEventName);
                    await donorImportRepository.InsertBatchOfDonors(donorInfoConversionResult.ProcessingResults);
                    return donorInfoConversionResult.FailedDonors;
                },
                "Imported donor batch.",
                logger,
                customProperties: new Dictionary<string, string> {{"BatchSize", BatchSize.ToString()}}
            );
        }
    }
}