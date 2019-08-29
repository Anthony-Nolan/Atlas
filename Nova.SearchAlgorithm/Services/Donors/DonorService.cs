using AutoMapper;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
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
        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;

        public DonorService(
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IActiveRepositoryFactory repositoryFactory)
        {
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
            donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();
            donorInspectionRepository = repositoryFactory.GetDonorInspectionRepository();
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
            inputDonors = inputDonors.ToList();

            var existingDonorIds = (await GetExistingDonorIds(inputDonors)).ToList();
            var newDonors = inputDonors.Where(id => !existingDonorIds.Contains(id.DonorId));
            var updateDonors = inputDonors.Where(id => existingDonorIds.Contains(id.DonorId));

            await CreateDonorBatch(newDonors);
            await UpdateDonorBatch(updateDonors);
        }

        private async Task<IEnumerable<int>> GetExistingDonorIds(IEnumerable<InputDonor> inputDonors)
        {
            var existingDonors = await donorInspectionRepository.GetDonors(inputDonors.Select(d => d.DonorId));
            return existingDonors.Select(d => d.DonorId);
        }

        private async Task CreateDonorBatch(IEnumerable<InputDonor> newDonors)
        {
            newDonors = newDonors.ToList();

            if (newDonors.Any())
            {
                var donorsWithHla = await GetDonorsWithExpandedHla(newDonors);
                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(donorsWithHla.AsEnumerable());
            }
        }

        private async Task UpdateDonorBatch(IEnumerable<InputDonor> updateDonors)
        {
            updateDonors = updateDonors.ToList();

            if (updateDonors.Any())
            {
                var donorsWithHla = await GetDonorsWithExpandedHla(updateDonors);
                await donorUpdateRepository.UpdateDonorBatch(donorsWithHla.AsEnumerable());
            }
        }

        private async Task<InputDonorWithExpandedHla[]> GetDonorsWithExpandedHla(IEnumerable<InputDonor> inputDonors)
        {
            return await Task.WhenAll(inputDonors.Select(async d =>
                {
                    var hla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(new PhenotypeInfo<string>(d.HlaNames));
                    return CombineDonorAndExpandedHla(d, hla);
                }
            ));
        }

        private static InputDonorWithExpandedHla CombineDonorAndExpandedHla(InputDonor inputDonor,
            PhenotypeInfo<ExpandedHla> matchingHla)
        {
            return new InputDonorWithExpandedHla
            {
                DonorId = inputDonor.DonorId,
                DonorType = inputDonor.DonorType,
                RegistryCode = inputDonor.RegistryCode,
                MatchingHla = matchingHla,
            };
        }
    }
}