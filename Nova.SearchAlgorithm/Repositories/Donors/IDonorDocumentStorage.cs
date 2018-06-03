using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    public interface IDonorDocumentStorage
    {
        Task InsertDonor(InputDonor donor);
        Task UpdateDonorWithNewHla(InputDonor donor);
        Task<DonorResult> GetDonor(int donorId);
        Task<IEnumerable<PotentialHlaMatchRelation>> GetMatchesForDonor(int donorId);
        Task<IEnumerable<DonorResult>> AllDonors();
        Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(Locus locus, LocusSearchCriteria criteria);
        int HighestDonorId();
    }
}