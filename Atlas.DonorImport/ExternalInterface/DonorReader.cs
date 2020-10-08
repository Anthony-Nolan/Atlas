using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.Mapping;

namespace Atlas.DonorImport.ExternalInterface
{
    public interface IDonorReader
    {
        IEnumerable<Donor> StreamAllDonors();

        /// <summary>
        /// Fetch donors by internal *ATLAS IDs*
        /// </summary>
        /// <param name="donorIds">Atlas IDs to fetch</param>
        /// <returns>A dictionary of Donor ID to full donor information.</returns>
        Task<IReadOnlyDictionary<int, Donor>> GetDonors(IEnumerable<int> donorIds);
    }

    public class DonorReader : IDonorReader
    {
        private readonly IDonorReadRepository donorReadRepository;

        public DonorReader(IDonorReadRepository donorReadRepository)
        {
            this.donorReadRepository = donorReadRepository;
        }

        public IEnumerable<Donor> StreamAllDonors()
        {
            return donorReadRepository.StreamAllDonors().Select(d => d.ToPublicDonor());
        }

        public async Task<IReadOnlyDictionary<int, Donor>> GetDonors(IEnumerable<int> donorIds)
        {
            var donorsByIds = await donorReadRepository.GetDonorsByIds(donorIds.ToList());
            return donorsByIds.ToDictionary(d => d.Key, d => d.Value.ToPublicDonor());
        }
    }
}