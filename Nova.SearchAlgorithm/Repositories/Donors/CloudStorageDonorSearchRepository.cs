using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
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

        public async Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria matchRequest)
        {
            var results = await Task.WhenAll(
                FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, Locus.A, matchRequest.LocusMismatchA),
                FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, Locus.B, matchRequest.LocusMismatchB),
                FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, Locus.Drb1, matchRequest.LocusMismatchDRB1));

            var matchesAtA = results[0];
            var matchesAtB = results[1];
            var matchesAtDrb1 = results[2];

            var matches = await Task.WhenAll(matchesAtA.Union(matchesAtB).Union(matchesAtDrb1)
                .GroupBy(m => m.Key)
                .Select(g => new PotentialSearchResult
                {
                    Donor = new DonorResult { DonorId = g.Key },
                    TotalMatchCount = g.Sum(m => m.Value.MatchCount),
                    MatchDetailsAtLocusA = matchesAtA.ContainsKey(g.Key) ? matchesAtA[g.Key] : new LocusMatchDetails { MatchCount = 0 },
                    MatchDetailsAtLocusB = matchesAtB.ContainsKey(g.Key) ? matchesAtB[g.Key] : new LocusMatchDetails { MatchCount = 0 },
                    MatchDetailsAtLocusDrb1 = matchesAtDrb1.ContainsKey(g.Key) ? matchesAtDrb1[g.Key] : new LocusMatchDetails { MatchCount = 0 },
                })
                .Where(m => m.TotalMatchCount >= 6 - matchRequest.DonorMismatchCount)
                .Where(m => m.MatchDetailsAtLocusA.MatchCount >= 2 - matchRequest.LocusMismatchA.MismatchCount)
                .Where(m => m.MatchDetailsAtLocusB.MatchCount >= 2 - matchRequest.LocusMismatchB.MismatchCount)
                .Where(m => m.MatchDetailsAtLocusDrb1.MatchCount >= 2 - matchRequest.LocusMismatchDRB1.MismatchCount)
                .Select(async m =>
                {
                    // Augment each match with registry and other data from GetDonor(id)
                    // Performance could be improved here, but at least it happens in parallel,
                    // and only after filtering match results, not before.
                    m.Donor = await GetDonor(m.Donor.DonorId);
                    return m;
                }));
            
            return matches;
        }

        private async Task<IDictionary<int, LocusMatchDetails>> FindMatchesAtLocus(DonorType searchType, IEnumerable<RegistryCode> registriesToSearch, Locus locus, AlleleLevelLocusMatchCriteria criteria)
        {
            LocusSearchCriteria repoCriteria = new LocusSearchCriteria
            {
                SearchType = searchType,
                Registries = registriesToSearch,
                HlaNamesToMatchInPositionOne = criteria.HlaNamesToMatchInPositionOne,
                HlaNamesToMatchInPositionTwo = criteria.HlaNamesToMatchInPositionTwo,
            };

            var matches = (await donorDocumentRepository.GetDonorMatchesAtLocus(locus, repoCriteria))
                .GroupBy(m => m.DonorId)
                .ToDictionary(g => g.Key, LocusMatchFromGroup);

            return matches;
        }

        private bool DirectMatch(IEnumerable<PotentialHlaMatchRelation> matches)
        {
            return matches.Where(m => m.SearchTypePosition == TypePositions.One && m.MatchingTypePositions.HasFlag(TypePositions.One)).Any()
                && matches.Where(m => m.SearchTypePosition == TypePositions.Two && m.MatchingTypePositions.HasFlag(TypePositions.Two)).Any();
        }

        private bool CrossMatch(IEnumerable<PotentialHlaMatchRelation> matches)
        {
            return matches.Where(m => m.SearchTypePosition == TypePositions.One && m.MatchingTypePositions.HasFlag(TypePositions.Two)).Any()
                && matches.Where(m => m.SearchTypePosition == TypePositions.Two && m.MatchingTypePositions.HasFlag(TypePositions.One)).Any();
        }

        private LocusMatchDetails LocusMatchFromGroup(IGrouping<int, PotentialHlaMatchRelation> group)
        {
            return new LocusMatchDetails
            {
                MatchCount = DirectMatch(group) || CrossMatch(group) ? 2 : 1
            };
        }

        public Task<DonorResult> GetDonor(int donorId)
        {
            return donorDocumentRepository.GetDonor(donorId);
        }

        public Task InsertDonor(RawInputDonor donor)
        {
            return donorDocumentRepository.InsertDonor(donor);
        }

        public async Task AddOrUpdateDonor(InputDonor donor)
        {
            await donorDocumentRepository.InsertDonor(donor.ToRawInputDonor());
            await donorDocumentRepository.RefreshMatchingGroupsForExistingDonor(donor);
        }

        public IBatchQueryAsync<DonorResult> AllDonors()
        {
            return donorDocumentRepository.AllDonors();
        }

        public Task RefreshMatchingGroupsForExistingDonor(InputDonor donor)
        {
            return donorDocumentRepository.RefreshMatchingGroupsForExistingDonor(donor);
        }
    }
}
