using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.Data.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Data.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.Utils.Notifications;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
{
    /// <summary>
    /// Responsible for handling inbound donor inserts/updates.
    /// Differentiated from the `IDonorImportService` as it listens for inbound data, rather than polling an external service
    /// </summary>
    public interface IDonorService
    {
        Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds);
        Task CreateOrUpdateDonorBatch(IEnumerable<DonorInfo> donorInfos);
    }

    public class DonorService : IDonorService
    {
        private const string HlaExpansionAlertSummary = "HLA Expansion Failure(s) in Search Algorithm";

        private readonly IDonorUpdateRepository donorUpdateRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IDonorHlaExpander donorHlaExpander;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;

        public DonorService(
            // ReSharper disable once SuggestBaseTypeForParameter
            IActiveRepositoryFactory repositoryFactory,
            IDonorHlaExpander donorHlaExpander,
            IFailedDonorsNotificationSender failedDonorsNotificationSender)
        {
            donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();
            donorInspectionRepository = repositoryFactory.GetDonorInspectionRepository();
            this.donorHlaExpander = donorHlaExpander;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
        }

        public async Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();

            if (donorIds.Any())
            {
                await donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(donorIds);
            }
        }

        public async Task CreateOrUpdateDonorBatch(IEnumerable<DonorInfo> donorInfos)
        {
            donorInfos = donorInfos.ToList();

            if (!donorInfos.Any())
            {
                return;
            }

            var expansionResult = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorInfos);

            await CreateOrUpdateDonorsWithHla(expansionResult);

            await SendFailedDonorsAlert(expansionResult);
        }

        private async Task CreateOrUpdateDonorsWithHla(DonorBatchProcessingResult<DonorInfoWithExpandedHla> expansionResult)
        {
            var donorsWithHla = expansionResult.ProcessingResults.ToList();

            if (!donorsWithHla.Any())
            {
                return;
            }

            var existingDonorIds = (await GetExistingDonorIds(donorsWithHla)).ToList();
            var newDonors = donorsWithHla.Where(id => !existingDonorIds.Contains(id.DonorId));
            var updateDonors = donorsWithHla.Where(id => existingDonorIds.Contains(id.DonorId));

            await CreateDonorBatch(newDonors);
            await UpdateDonorBatch(updateDonors);
        }

        private async Task<IEnumerable<int>> GetExistingDonorIds(IEnumerable<DonorInfoWithExpandedHla> donorInfos)
        {
            var existingDonors = await donorInspectionRepository.GetDonors(donorInfos.Select(d => d.DonorId));
            return existingDonors.Keys;
        }

        private async Task CreateDonorBatch(IEnumerable<DonorInfoWithExpandedHla> newDonors)
        {
            newDonors = newDonors.ToList();

            if (newDonors.Any())
            {
                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(newDonors.AsEnumerable());
            }
        }

        private async Task UpdateDonorBatch(IEnumerable<DonorInfoWithExpandedHla> updateDonors)
        {
            updateDonors = updateDonors.ToList();

            if (updateDonors.Any())
            {
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
                HlaExpansionAlertSummary,
                Priority.Medium);
        }
    }
}