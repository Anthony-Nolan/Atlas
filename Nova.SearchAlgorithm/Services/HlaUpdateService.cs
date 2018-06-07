using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
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
            var batch = donorInspectionRepository.AllDonors();

            while (batch.HasMoreResults)
            {
                var results = await batch.RequestNextAsync();

                await Task.WhenAll(results.Select(UpdateSingleDonorHlaAsync));
            }
        }

        private async Task UpdateSingleDonorHlaAsync(DonorResult donor)
        {
            var update = new InputDonor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                MatchingHla = (await donor.HlaNames
                                  .WhenAllPositions((l, p, n) => n == null ? null : lookupService.GetMatchingHla(l.ToMatchLocus(), n))
                              ).Map((l, p, n) => n.ToExpandedHla())
            };
            await donorImportRepository.RefreshMatchingGroupsForExistingDonor(update);
        }
    }
}