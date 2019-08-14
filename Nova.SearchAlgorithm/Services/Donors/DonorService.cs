using AutoMapper;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.Http.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
{
    /// <summary>
    /// Responsible for handling inbound donor inserts/updates.
    /// Differentiated from the `IDonorImportService` as it listens for inbound data, rather than polling an external service
    /// </summary>
    public interface IDonorService
    {
        Task<InputDonor> CreateDonor(InputDonor inputDonor);
        Task<InputDonor> UpdateDonor(InputDonor inputDonor);
        Task DeleteDonorBatch(IEnumerable<int> donorIds);
        Task<IEnumerable<InputDonor>> CreateDonorBatch(IEnumerable<InputDonor> inputDonors);
        Task<IEnumerable<InputDonor>> UpdateDonorBatch(IEnumerable<InputDonor> inputDonor);
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

        public async Task<InputDonor> CreateDonor(InputDonor inputDonor)
        {
            return (await CreateDonorBatch(new[] { inputDonor })).Single();
        }

        public async Task<InputDonor> UpdateDonor(InputDonor inputDonor)
        {
            return (await UpdateDonorBatch(new[] { inputDonor })).Single();
        }

        public async Task DeleteDonorBatch(IEnumerable<int> donorIds)
        {
            await donorUpdateRepository.DeleteDonorBatch(donorIds);
        }

        public async Task<IEnumerable<InputDonor>> CreateDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            inputDonors = inputDonors.ToList();
            var existingDonors = (await donorInspectionRepository.GetDonors(inputDonors.Select(d => d.DonorId))).ToList();
            if (existingDonors.Any())
            {
                throw new NovaHttpException(
                    HttpStatusCode.Conflict,
                    $"One or more donors already exist. Donor ID(s): {string.Join(",", existingDonors.Select(d => d.DonorId))}"
                );
            }

            var donorsWithHla = await Task.WhenAll(inputDonors.Select(async d =>
                {
                    var hla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(new PhenotypeInfo<string>(d.HlaNames));
                    return CombineDonorAndExpandedHla(d, hla);
                }
            ));
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(donorsWithHla.AsEnumerable());

            return await GetDonors(inputDonors.Select(d => d.DonorId));
        }

        public async Task<IEnumerable<InputDonor>> CreateOrUpdateDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            inputDonors = inputDonors.ToList();
            var existingDonors = (await donorInspectionRepository.GetDonors(inputDonors.Select(d => d.DonorId)));
            var updateDonors = inputDonors.Where(id => existingDonors.Any(ed => ed.DonorId == id.DonorId)).ToList();
            var newDonors = inputDonors.Where(id => existingDonors.All(ed => ed.DonorId != id.DonorId)).ToList();

            if (newDonors.Any())
            {
                await CreateDonorBatch(newDonors);
            }

            if (updateDonors.Any())
            {
                await UpdateDonorBatch(updateDonors);
            }

            return await GetDonors(inputDonors.Select(d => d.DonorId));
        }

        public async Task<IEnumerable<InputDonor>> UpdateDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            inputDonors = inputDonors.ToList();
            var existingDonors = (await donorInspectionRepository.GetDonors(inputDonors.Select(d => d.DonorId))).ToList();
            if (existingDonors.Count() != inputDonors.Count())
            {
                var newDonors = inputDonors.Where(id => existingDonors.All(ed => ed.DonorId != id.DonorId));
                throw new NovaNotFoundException(
                    $"One or more donors do not exist. Donor ID(s):  {string.Join(",", newDonors.Select(d => d.DonorId))}");
            }

            var donorsWithHla = await Task.WhenAll(inputDonors.Select(async d =>
                {
                    var hla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(new PhenotypeInfo<string>(d.HlaNames));
                    return CombineDonorAndExpandedHla(d, hla);
                }
            ));
            await donorUpdateRepository.UpdateBatchOfDonorsWithExpandedHla(donorsWithHla.AsEnumerable());

            return await GetDonors(inputDonors.Select(d => d.DonorId));
        }

        private InputDonorWithExpandedHla CombineDonorAndExpandedHla(InputDonor inputDonor,
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

        private async Task<IEnumerable<InputDonor>> GetDonors(IEnumerable<int> donorIds)
        {
            var donors = await donorInspectionRepository.GetDonors(donorIds);
            return donors.Select(donor => mapper.Map<InputDonor>(donor));
        }
    }
}