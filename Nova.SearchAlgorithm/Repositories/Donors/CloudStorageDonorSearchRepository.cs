using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    public class CloudStorageDonorSearchRepository : IDonorSearchRepository, IDonorImportRepository, IDonorInspectionRepository
    {
        private readonly IDonorDocumentStorage donorDocumentRepository;

        public CloudStorageDonorSearchRepository(IDonorDocumentStorage donorDocumentRepository)
        {
            this.donorDocumentRepository = donorDocumentRepository;
        }

        public Task<int> HighestDonorId()
        {
            return donorDocumentRepository.HighestDonorId();
        }

        public async Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(Locus locus, LocusSearchCriteria criteria)
        {
            return await donorDocumentRepository.GetDonorMatchesAtLocus(locus, criteria);
        }

        public Task<DonorResult> GetDonor(int donorId)
        {
            return donorDocumentRepository.GetDonor(donorId);
        }

        public Task<PhenotypeInfo<IEnumerable<string>>> GetPGroupsForDonor(int donorId)
        {
            throw new System.NotImplementedException();
        }

        public Task InsertDonor(RawInputDonor donor)
        {
            return donorDocumentRepository.InsertDonor(donor);
        }

        public Task InsertBatchOfDonors(IEnumerable<RawInputDonor> donors)
        {
            return donorDocumentRepository.InsertBatchOfDonors(donors);
        }

        public async Task AddOrUpdateDonorWithHla(InputDonor donor)
        {
            await donorDocumentRepository.RefreshMatchingGroupsForExistingDonor(donor);
        }

        public void SetupForHlaRefresh()
        {
            donorDocumentRepository.SetupForHlaRefresh();
        }

        public IBatchQueryAsync<DonorResult> AllDonors()
        {
            return donorDocumentRepository.AllDonors();
        }

        private Task RefreshMatchingGroupsForExistingDonor(InputDonor donor)
        {
            return donorDocumentRepository.RefreshMatchingGroupsForExistingDonor(donor);
        }

        public Task RefreshMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonor> donors)
        {
            return Task.WhenAll(donors.Select(RefreshMatchingGroupsForExistingDonor));
        }

        public void InsertPGroups(IEnumerable<string> pGroups)
        {
            // This is not necessary in non-relational databases, as the PGroups are stored as strings on the match entities
        }
    }
}
