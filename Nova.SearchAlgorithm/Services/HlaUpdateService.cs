using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;

namespace Nova.SearchAlgorithm.Services
{
    public class HlaUpdateService : IHlaUpdateService
    {
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IDonorImportRepository donorImportRepository;

        public HlaUpdateService(IMatchingDictionaryLookupService lookupService, IDonorInspectionRepository donorInspectionRepository, IDonorImportRepository donorImportRepository)
        {
            this.lookupService = lookupService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.donorImportRepository = donorImportRepository;
        }

        public async Task UpdateDonorHla()
        {
            await Task.WhenAll((await donorInspectionRepository.AllDonors()).Select(donor =>
            {
                var update = new InputDonor
                {
                    DonorId = donor.DonorId,
                    DonorType = donor.DonorType,
                    RegistryCode = donor.RegistryCode,
                    MatchingHla = donor.HlaNames.Map((l, p, n) =>
                        n == null ? null : lookupService.GetMatchingHla(l.ToMatchLocus(), n).Result.ToExpandedHla())
                };
                return donorImportRepository.RefreshMatchingGroupsForExistingDonor(update);
            }));
        }
    }
}