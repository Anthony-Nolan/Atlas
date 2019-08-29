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
        Task<IEnumerable<InputDonor>> CreateOrUpdateDonorBatch(IEnumerable<InputDonor> inputDonors);
    }

    public class DonorService : IDonorService
    {
        private readonly IDonorUpdateRepository donorUpdateRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private readonly IMapper mapper;

        public DonorService(
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IActiveRepositoryFactory repositoryFactory,
            IMapper mapper)
        {
            this.mapper = mapper;
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
            donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();
            donorInspectionRepository = repositoryFactory.GetDonorInspectionRepository();
        }

        public async Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds)
        {
            await donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(donorIds);
        }

        public async Task<IEnumerable<InputDonor>> CreateOrUpdateDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            inputDonors = inputDonors.ToList();

            var existingDonorIds = (await GetExistingDonorIds(inputDonors)).ToList();

            await CreateDonorBatch(existingDonorIds, inputDonors);
            await UpdateDonorBatch(existingDonorIds, inputDonors);

            var results = await GetDonorResults(inputDonors);
            return mapper.Map<IEnumerable<InputDonor>>(results);
        }

        private async Task<IEnumerable<int>> GetExistingDonorIds(IEnumerable<InputDonor> inputDonors)
        {
            var existingDonors = await GetDonorResults(inputDonors);
            return existingDonors.Select(d => d.DonorId);
        }

        private async Task<IEnumerable<DonorResult>> GetDonorResults(IEnumerable<InputDonor> inputDonors)
        {
            return await donorInspectionRepository.GetDonors(inputDonors.Select(d => d.DonorId));
        }

        private async Task CreateDonorBatch(IEnumerable<int> existingDonorIds, IEnumerable<InputDonor> inputDonors)
        {
            var newDonors = inputDonors.Where(id => !existingDonorIds.Contains(id.DonorId)).ToList();

            if (newDonors.Any())
            {
                var donorsWithHla = await GetDonorsWithExpandedHla(newDonors);
                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(donorsWithHla.AsEnumerable());
            }
        }

        private async Task UpdateDonorBatch(IEnumerable<int> existingDonorIds, IEnumerable<InputDonor> inputDonors)
        {
            var updateDonors = inputDonors.Where(id => existingDonorIds.Contains(id.DonorId)).ToList();

            if (updateDonors.Any())
            {
                await UpdateDonorHlaBatch(updateDonors);
            }
        }

        private async Task UpdateDonorHlaBatch(IEnumerable<InputDonor> inputDonors)
        {
            var donorsWithHla = await GetDonorsWithExpandedHla(inputDonors);
            await donorUpdateRepository.UpdateDonorBatch(donorsWithHla.AsEnumerable());
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