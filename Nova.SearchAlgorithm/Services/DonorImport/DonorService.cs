using AutoMapper;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.Http.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DonorImport
{
    public interface IDonorService
    {
        Task<InputDonor> CreateDonor(InputDonor inputDonor);
        Task<InputDonor> UpdateDonor(InputDonor inputDonor);
        Task DeleteDonor(int donorId);
        Task<IEnumerable<InputDonor>> CreateDonorBatch(IEnumerable<InputDonor> inputDonors);
        Task<IEnumerable<InputDonor>> UpdateDonorBatch(IEnumerable<InputDonor> inputDonor);
        Task<IEnumerable<InputDonor>> CreateOrUpdateDonorBatch(IEnumerable<InputDonor> inputDonors);
    }

    public class DonorService : IDonorService
    {
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private readonly IMapper mapper;

        public DonorService(
            IDonorImportRepository donorImportRepository,
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            IDonorInspectionRepository donorInspectionRepository,
            IMapper mapper)
        {
            this.donorImportRepository = donorImportRepository;
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.mapper = mapper;
        }

        public async Task<InputDonor> CreateDonor(InputDonor inputDonor)
        {
            return (await CreateDonorBatch(new[] { inputDonor })).Single();
        }

        public async Task<InputDonor> UpdateDonor(InputDonor inputDonor)
        {
            return (await UpdateDonorBatch(new[] { inputDonor })).Single();
        }

        public async Task DeleteDonor(int donorId)
        {
            var existingDonors = await donorInspectionRepository.GetDonors(new[] { donorId });

            if (!existingDonors.Any())
            {
                throw new NovaNotFoundException($"Donor ID {donorId} does not exist in the database.");
            }

            await donorImportRepository.DeleteDonorAndItsExpandedHla(donorId);
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
            await donorImportRepository.InsertBatchOfDonorsWithExpandedHla(donorsWithHla.AsEnumerable());

            return await GetDonors(inputDonors.Select(d => d.DonorId));
        }

        public async Task<IEnumerable<InputDonor>> CreateOrUpdateDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            var existingDonors = (await donorInspectionRepository.GetDonors(inputDonors.Select(d => d.DonorId)));
            var updateDonors = inputDonors.Where(id => existingDonors.Any(ed => ed.DonorId == id.DonorId));
            var newDonors = inputDonors.Where(id => existingDonors.All(ed => ed.DonorId != id.DonorId));

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
            await donorImportRepository.UpdateBatchOfDonorsWithExpandedHla(donorsWithHla.AsEnumerable());

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