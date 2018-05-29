using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;

namespace Nova.SearchAlgorithm.Services
{
    public interface IHlaUpdateService
    {
        void UpdateDonorHla();
    }
    public class HlaUpdateService : IHlaUpdateService
    {
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly IDonorMatchRepository donorRepository;

        public HlaUpdateService(IMatchingDictionaryLookupService lookupService, IDonorMatchRepository donorRepository)
        {
            this.lookupService = lookupService;
            this.donorRepository = donorRepository;
        }

        public void UpdateDonorHla()
        {
            foreach (var donor in donorRepository.AllDonors())
            {
                var update = new InputDonor
                {
                    DonorId = donor.DonorId,
                    DonorType = donor.DonorType,
                    RegistryCode = donor.RegistryCode,
                    MatchingHla = donor.HlaNames.Map((l, p, n) => n == null ? null : lookupService.GetMatchingHla(l.ToMatchLocus(), n).Result.ToExpandedHla())
                };
                donorRepository.UpdateDonorWithNewHla(update);
            }
        }
    }
}