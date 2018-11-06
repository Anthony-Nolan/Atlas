using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.Utils.Http.Exceptions;

namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorService
    {
        Task<InputDonor> CreateDonor(InputDonor inputDonor);
        Task<InputDonor> UpdateDonor(InputDonor inputDonor);
        Task<IEnumerable<InputDonor>> CreateDonorBatch(IEnumerable<InputDonor> inputDonors);
        Task<IEnumerable<InputDonor>> UpdateDonorBatch(IEnumerable<InputDonor> inputDonor);
    }

    public class DonorService : IDonorService
    {
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;

        public DonorService(
            IDonorImportRepository donorImportRepository,
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            IDonorInspectionRepository donorInspectionRepository
        )
        {
            this.donorImportRepository = donorImportRepository;
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
            this.donorInspectionRepository = donorInspectionRepository;
        }

        public async Task<InputDonor> CreateDonor(InputDonor inputDonor)
        {
            var donorId = inputDonor.DonorId;
            var donorExists = await donorInspectionRepository.GetDonor(donorId) != null;
            if (donorExists)
            {
                throw new NovaHttpException(HttpStatusCode.Conflict, $"Donor {donorId} already exists");
            }

            var matchingHla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(new PhenotypeInfo<string>(inputDonor.HlaNames));
            await donorImportRepository.InsertDonorWithHla(CombineDonorAndExpandedHla(inputDonor, matchingHla));

            return await GetDonor(inputDonor.DonorId);
        }

        public async Task<InputDonor> UpdateDonor(InputDonor inputDonor)
        {
            var donorId = inputDonor.DonorId;
            var existingDonor = await donorInspectionRepository.GetDonor(donorId);
            var donorExists = existingDonor != null;
            if (!donorExists)
            {
                throw new NovaNotFoundException($"Donor {donorId} does not exist");
            }

            var donorDetailsUnchanged = existingDonor.DonorType == inputDonor.DonorType && existingDonor.RegistryCode == inputDonor.RegistryCode;
            var hlaUnchanged = existingDonor.HlaNames.Equals(new PhenotypeInfo<string>(inputDonor.HlaNames));
            if (donorDetailsUnchanged && hlaUnchanged)
            {
                return inputDonor;
            }

            var matchingHla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(new PhenotypeInfo<string>(inputDonor.HlaNames));
            await donorImportRepository.UpdateDonorWithHla(CombineDonorAndExpandedHla(inputDonor, matchingHla));

            return await GetDonor(inputDonor.DonorId);
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
            await donorImportRepository.InsertBatchOfDonorsWithHla(donorsWithHla.AsEnumerable());

            return await GetDonors(inputDonors.Select(d => d.DonorId));
        }

        public async Task<IEnumerable<InputDonor>> UpdateDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            inputDonors = inputDonors.ToList();
            var existingDonors = (await donorInspectionRepository.GetDonors(inputDonors.Select(d => d.DonorId))).ToList();
            if (existingDonors.Count() != inputDonors.Count())
            {
                var newDonors = inputDonors.Where(id => existingDonors.All(ed => ed.DonorId != id.DonorId));
                throw new NovaNotFoundException($"One or more donors do not exist. Donor ID(s):  {string.Join(",", newDonors.Select(d => d.DonorId))}");
            }
            
            var donorsWithHla = await Task.WhenAll(inputDonors.Select(async d =>
                {
                    var hla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(new PhenotypeInfo<string>(d.HlaNames));
                    return CombineDonorAndExpandedHla(d, hla);
                }
            ));
            await donorImportRepository.UpdateBatchOfDonorsWithHla(donorsWithHla.AsEnumerable());

            return await GetDonors(inputDonors.Select(d => d.DonorId));
        }

        private static InputDonorWithExpandedHla CombineDonorAndExpandedHla(InputDonor inputDonor, PhenotypeInfo<ExpandedHla> matchingHla)
        {
            return new InputDonorWithExpandedHla
            {
                DonorId = inputDonor.DonorId,
                DonorType = inputDonor.DonorType,
                RegistryCode = inputDonor.RegistryCode,
                MatchingHla = matchingHla,
            };
        }

        private async Task<InputDonor> GetDonor(int donorId)
        {
            var donor = await donorInspectionRepository.GetDonor(donorId);
            return new InputDonor
            {
                DonorId = donor.DonorId,
                HlaNames = donor.HlaNames,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
            };
        }

        private async Task<IEnumerable<InputDonor>> GetDonors(IEnumerable<int> donorIds)
        {
            var donors = await donorInspectionRepository.GetDonors(donorIds);
            return donors.Select(donor => new InputDonor
            {
                DonorId = donor.DonorId,
                HlaNames = donor.HlaNames,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
            });
        }
    }
}