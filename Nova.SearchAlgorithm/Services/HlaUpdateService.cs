using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Hla;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Services
{
    public interface IHlaUpdateService
    {
        void UpdateDonorHla();
    }
    public class HlaUpdateService : IHlaUpdateService
    {
        private readonly IHlaRepository hlaRepository;
        private readonly IDonorRepository donorRepository;

        public HlaUpdateService(IHlaRepository hlaRepository, IDonorRepository donorRepository)
        {
            this.hlaRepository = hlaRepository;
            this.donorRepository = donorRepository;
        }

        public void UpdateDonorHla()
        {
            foreach (var donor in donorRepository.AllDonors())
            {
                // TODO:NOVA-919 MatchingHla shouldn't be null
                if (donor.MatchingHla != null)
                {
                    donor.MatchingHla = donor.MatchingHla.Map((l, p, n) => n == null ? null : hlaRepository.RetrieveHlaMatches(l, n.Name));
                    donorRepository.UpdateDonorWithNewHla(donor);
                }
            }
        }
    }
}