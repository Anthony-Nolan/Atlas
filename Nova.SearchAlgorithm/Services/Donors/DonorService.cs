using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
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
        Task CreateOrUpdateDonorBatch(IEnumerable<InputDonor> inputDonors);
    }

    public class DonorService : IDonorService
    {
        private readonly IDonorUpdateRepository donorUpdateRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IDonorValidator donorValidator;
        private readonly IDonorHlaExpander donorHlaExpander;

        public DonorService(
            // ReSharper disable once SuggestBaseTypeForParameter
            IActiveRepositoryFactory repositoryFactory,
            IDonorValidator donorValidator,
            IDonorHlaExpander donorHlaExpander)
        {
            donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();
            donorInspectionRepository = repositoryFactory.GetDonorInspectionRepository();
            this.donorValidator = donorValidator;
            this.donorHlaExpander = donorHlaExpander;
        }

        public async Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();

            if (donorIds.Any())
            {
                await donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(donorIds);
            }
        }

        public async Task CreateOrUpdateDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            var validDonors = (await donorValidator.ValidateDonorsAsync(inputDonors)).ToList();

            if (validDonors.Any())
            {
                var donorsWithHla = (await donorHlaExpander.ExpandDonorHlaBatchAsync(validDonors)).ToList();

                var existingDonorIds = (await GetExistingDonorIds(donorsWithHla)).ToList();
                var newDonors = donorsWithHla.Where(id => !existingDonorIds.Contains(id.DonorId));
                var updateDonors = donorsWithHla.Where(id => existingDonorIds.Contains(id.DonorId));

                await CreateDonorBatch(newDonors);
                await UpdateDonorBatch(updateDonors);
            }
        }

        private async Task<IEnumerable<int>> GetExistingDonorIds(IEnumerable<InputDonorWithExpandedHla> inputDonors)
        {
            var existingDonors = await donorInspectionRepository.GetDonors(inputDonors.Select(d => d.DonorId));
            return existingDonors.Keys;
        }

        private async Task CreateDonorBatch(IEnumerable<InputDonorWithExpandedHla> newDonors)
        {
            newDonors = newDonors.ToList();

            if (newDonors.Any())
            {
                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(newDonors.AsEnumerable());
            }
        }

        private async Task UpdateDonorBatch(IEnumerable<InputDonorWithExpandedHla> updateDonors)
        {
            updateDonors = updateDonors.ToList();

            if (updateDonors.Any())
            {
                await donorUpdateRepository.UpdateDonorBatch(updateDonors.AsEnumerable());
            }
        }
    }
}