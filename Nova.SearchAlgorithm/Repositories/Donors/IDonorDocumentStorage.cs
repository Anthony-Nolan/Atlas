using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    public interface IDonorDocumentStorage
    {
        Task InsertDonor(InputDonor donor);
        Task UpdateDonorWithNewHla(InputDonor donor);
        Task<DonorResult> GetDonor(int donorId);
        Task<IEnumerable<DonorResult>> AllDonors();
        Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(Locus locus, LocusSearchCriteria criteria);
        Task<int> HighestDonorId();
    }
}