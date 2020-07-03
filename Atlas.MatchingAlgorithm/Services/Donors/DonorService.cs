using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    /// <summary>
    /// Responsible for handling inbound donor inserts/updates.
    /// Differentiated from the `IDonorImportService` as it listens for inbound data, rather than polling an external service
    /// </summary>
    public interface IDonorService
    {
        Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds, TransientDatabase targetDatabase);

        /// <param name="hlaNomenclatureVersion">
        ///  This method includes processing the HLA, thus we need to know which version of the HLA nomenclature to be using for that interpretation
        /// </param>
        Task CreateOrUpdateDonorBatch(IEnumerable<DonorInfo> donorInfos, TransientDatabase targetDatabase, string hlaNomenclatureVersion);
    }

    public class DonorService : IDonorService
    {
        private const string ExpansionFailureEventName = "HLA Expansion Failure(s) in Matching Algorithm's Continuous Donor Update sytem";

        private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
        private readonly IDonorHlaExpanderFactory donorHlaExpanderFactory;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;

        public DonorService(
            IStaticallyChosenDatabaseRepositoryFactory repositoryFactory,
            IDonorHlaExpanderFactory donorHlaExpanderFactory,
            IFailedDonorsNotificationSender failedDonorsNotificationSender)
        {
            this.repositoryFactory = repositoryFactory;
            this.donorHlaExpanderFactory = donorHlaExpanderFactory;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
        }

        public async Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds, TransientDatabase targetDatabase)
        {
            donorIds = donorIds.ToList();

            if (donorIds.Any())
            {
                var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepositoryForDatabase(targetDatabase);

                await donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(donorIds);
            }
        }

        public async Task CreateOrUpdateDonorBatch(
            IEnumerable<DonorInfo> donorInfos,
            TransientDatabase targetDatabase,
            string hlaNomenclatureVersion)
        {
            donorInfos = donorInfos.ToList();

            if (!donorInfos.Any())
            {
                return;
            }
            var donorHlaExpander = donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersion);
            var expansionResult = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorInfos, ExpansionFailureEventName);

            await CreateOrUpdateDonorsWithHla(expansionResult, targetDatabase);

            await SendFailedDonorsAlert(expansionResult);
        }

        private async Task CreateOrUpdateDonorsWithHla(DonorBatchProcessingResult<DonorInfoWithExpandedHla> expansionResult, TransientDatabase targetDatabase)
        {
            var donorsWithHla = expansionResult.ProcessingResults.ToList();

            if (!donorsWithHla.Any())
            {
                return;
            }

            var existingDonorIds = (await GetExistingDonorIds(donorsWithHla, targetDatabase)).ToList();
            var newDonors = donorsWithHla.Where(id => !existingDonorIds.Contains(id.DonorId));
            var updateDonors = donorsWithHla.Where(id => existingDonorIds.Contains(id.DonorId));

            await CreateDonorBatch(newDonors, targetDatabase);
            await UpdateDonorBatch(updateDonors, targetDatabase);
        }

        private async Task<IEnumerable<int>> GetExistingDonorIds(IEnumerable<DonorInfoWithExpandedHla> donorInfos, TransientDatabase targetDatabase)
        {
            var donorInspectionRepository = repositoryFactory.GetDonorInspectionRepositoryForDatabase(targetDatabase);

            var existingDonors = await donorInspectionRepository.GetDonors(donorInfos.Select(d => d.DonorId));
            return existingDonors.Keys;
        }

        private async Task CreateDonorBatch(IEnumerable<DonorInfoWithExpandedHla> newDonors, TransientDatabase targetDatabase)
        {
            newDonors = newDonors.ToList();

            if (newDonors.Any())
            {
                var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepositoryForDatabase(targetDatabase);

                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(newDonors.AsEnumerable());
            }
        }

        private async Task UpdateDonorBatch(IEnumerable<DonorInfoWithExpandedHla> updateDonors, TransientDatabase targetDatabase)
        {
            updateDonors = updateDonors.ToList();

            if (updateDonors.Any())
            {
                var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepositoryForDatabase(targetDatabase);

                await donorUpdateRepository.UpdateDonorBatch(updateDonors.AsEnumerable());
            }
        }

        private async Task SendFailedDonorsAlert(DonorBatchProcessingResult<DonorInfoWithExpandedHla> expansionResult)
        {
            var failedDonors = expansionResult.FailedDonors.ToList();

            if (!failedDonors.Any())
            {
                return;
            }

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                failedDonors,
                ExpansionFailureEventName,
                Priority.Medium);
        }
    }
}