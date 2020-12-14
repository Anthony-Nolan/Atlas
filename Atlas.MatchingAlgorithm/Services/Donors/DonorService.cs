using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Microsoft.Extensions.Primitives;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    /// <summary>
    /// Responsible for handling inbound donor inserts/updates.
    /// Differentiated from the `IDonorImportService` as it listens for inbound data, rather than polling an external service
    /// </summary>
    public interface IDonorService
    {
        Task SetDonorBatchAsUnavailableForSearch(List<int> donorIds, TransientDatabase targetDatabase);

        /// <param name="hlaNomenclatureVersion">
        ///  This method includes processing the HLA, thus we need to know which version of the HLA nomenclature to be using for that interpretation
        /// </param>
        Task CreateOrUpdateDonorBatch(IEnumerable<DonorInfo> donorInfos, TransientDatabase targetDatabase, string hlaNomenclatureVersion, bool runAllHlaInsertionsInASingleTransactionScope);
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

        public async Task SetDonorBatchAsUnavailableForSearch(List<int> donorIds, TransientDatabase targetDatabase)
        {
            if (donorIds.Any())
            {
                var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepositoryForDatabase(targetDatabase);

                await donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(donorIds);
            }
        }

        public async Task CreateOrUpdateDonorBatch(
            IEnumerable<DonorInfo> donorInfos,
            TransientDatabase targetDatabase,
            string hlaNomenclatureVersion,
            bool runAllHlaInsertionsInASingleTransactionScope)
        {
            donorInfos = donorInfos.ToList();

            if (!donorInfos.Any())
            {
                return;
            }
            var donorHlaExpander = donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersion);
            var expansionResult = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorInfos, ExpansionFailureEventName);
            EnsureAllPGroupsExist(expansionResult.ProcessingResults, targetDatabase);

            await CreateOrUpdateDonorsWithHla(expansionResult.ProcessingResults, targetDatabase, runAllHlaInsertionsInASingleTransactionScope);
            await SendFailedDonorsAlert(expansionResult.FailedDonors);
        }

        /// <remarks>
        /// See notes in FindOrCreatePGroupIds.
        /// In practice this will never do anything in Prod code.
        /// But it means that during tests the DonorUpdate code behaves more like
        /// "the real thing", since the PGroups have already been inserted into the DB.
        /// </remarks>
        private void EnsureAllPGroupsExist(
            IReadOnlyCollection<DonorInfoWithExpandedHla> donorsWithHlas,
            TransientDatabase targetDatabase)
        {
            var pGroupRepo = repositoryFactory.GetPGroupRepositoryForDatabase(targetDatabase);
            var allPGroups = donorsWithHlas
                .SelectMany(d =>
                    d.MatchingHla?.ToEnumerable().SelectMany(hla => hla?.MatchingPGroups ?? new string[0]) ?? new List<string>()
                ).ToList();

            pGroupRepo.EnsureAllPGroupsExist(allPGroups);
        }

        private async Task CreateOrUpdateDonorsWithHla(IReadOnlyCollection<DonorInfoWithExpandedHla> donorsWithHla, TransientDatabase targetDatabase, bool runAllHlaInsertionsInASingleTransactionScope)
        {
            if (!donorsWithHla.Any())
            {
                return;
            }

            var existingDonorIds = (await GetExistingDonorIds(donorsWithHla, targetDatabase)).ToList();
            var (updatedDonors, newDonors) = donorsWithHla.ReifyAndSplit(id => existingDonorIds.Contains(id.DonorId));

            using (var transactionScope = new OptionalAsyncTransactionScope(runAllHlaInsertionsInASingleTransactionScope))
            {
                await CreateDonorBatch(newDonors, targetDatabase, runAllHlaInsertionsInASingleTransactionScope);
                await UpdateDonorBatch(updatedDonors, targetDatabase, runAllHlaInsertionsInASingleTransactionScope);
                transactionScope.Complete();
            }
        }

        private async Task<IEnumerable<int>> GetExistingDonorIds(IEnumerable<DonorInfoWithExpandedHla> donorInfos, TransientDatabase targetDatabase)
        {
            var donorInspectionRepository = repositoryFactory.GetDonorInspectionRepositoryForDatabase(targetDatabase);

            var existingDonors = await donorInspectionRepository.GetDonors(donorInfos.Select(d => d.DonorId));
            return existingDonors.Keys;
        }

        private async Task CreateDonorBatch(List<DonorInfoWithExpandedHla> newDonors, TransientDatabase targetDatabase, bool runAllHlaInsertionsInASingleTransactionScope)
        {
            if (newDonors.Any())
            {
                var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepositoryForDatabase(targetDatabase);

                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(newDonors, runAllHlaInsertionsInASingleTransactionScope);
            }
        }

        private async Task UpdateDonorBatch(List<DonorInfoWithExpandedHla> updateDonors, TransientDatabase targetDatabase, bool runAllHlaInsertionsInASingleTransactionScope)
        {
            if (updateDonors.Any())
            {
                var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepositoryForDatabase(targetDatabase);

                await donorUpdateRepository.UpdateDonorBatch(updateDonors, runAllHlaInsertionsInASingleTransactionScope);
            }
        }

        private async Task SendFailedDonorsAlert(IReadOnlyCollection<FailedDonorInfo> failedDonors)
        {

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