using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Hla;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;

namespace Nova.SearchAlgorithm.Services
{
    public interface IHlaUpdateService
    {
        void UpdateDonorHla();
    }
    public class HlaUpdateService : IHlaUpdateService
    {
        private readonly IHlaRepository hlaRepository;
        private readonly IDonorMatchRepository donorRepository;

        public HlaUpdateService(IHlaRepository hlaRepository, IDonorMatchRepository donorRepository)
        {
            this.hlaRepository = hlaRepository;
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
                    MatchingHla = donor.HlaNames.Map((l, p, n) => n == null ? null : hlaRepository.RetrieveHlaMatches(l, n))
                };
                donorRepository.UpdateDonorWithNewHla(update);
            }
        }
    }
}