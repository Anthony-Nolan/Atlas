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
            await donorImportRepository.AddDonorWithHla(CombineDonorAndExpandedHla(inputDonor, matchingHla));

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
    }
}