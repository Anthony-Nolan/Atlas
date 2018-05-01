using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    public class CloudStorageDonorMatchRepository : IDonorMatchRepository
    {
        private readonly IDonorCloudTables donorBlobRepository;

        public CloudStorageDonorMatchRepository(IDonorCloudTables donorBlobRepository)
        {
            this.donorBlobRepository = donorBlobRepository;
        }

        public IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest)
        {
            var matchesAtA = FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, "A", matchRequest.LocusMismatchA);
            var matchesAtB = FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, "B", matchRequest.LocusMismatchB);
            var matchesAtDRB1 = FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, "DRB1", matchRequest.LocusMismatchDRB1);

            var matches = matchesAtA.Union(matchesAtB).Union(matchesAtDRB1)
                .GroupBy(m => m.Key)
                .Select(g => new PotentialMatch
                {
                    DonorId = g.Key,
                    TotalMatchCount = g.Sum(m => m.Value.MatchCount ?? 0),
                    MatchDetailsAtLocusA = matchesAtA.ContainsKey(g.Key) ? matchesAtA[g.Key] : new LocusMatchDetails { MatchCount = 0 },
                    MatchDetailsAtLocusB = matchesAtB.ContainsKey(g.Key) ? matchesAtB[g.Key] : new LocusMatchDetails { MatchCount = 0 },
                    MatchDetailsAtLocusDRB1 = matchesAtDRB1.ContainsKey(g.Key) ? matchesAtDRB1[g.Key] : new LocusMatchDetails { MatchCount = 0 },
                })
                .Where(m => m.TotalMatchCount >= 6 - matchRequest.DonorMismatchCountTier1)
                .Where(m => m.MatchDetailsAtLocusA.MatchCount >= 2 - matchRequest.LocusMismatchA.MismatchCount)
                .Where(m => m.MatchDetailsAtLocusB.MatchCount >= 2 - matchRequest.LocusMismatchB.MismatchCount)
                .Where(m => m.MatchDetailsAtLocusDRB1.MatchCount >= 2 - matchRequest.LocusMismatchDRB1.MismatchCount)
                .Select(m =>
                {
                    // Augment each match with registry and other data from GetDonor(id)
                    // Performance could be improved here
                    var donor = GetDonor(m.DonorId);
                    m.Registry = donor.RegistryCode;
                    m.DonorType = donor.DonorType;
                    return m;
                });
            
            return matches;
        }

        private IDictionary<int, LocusMatchDetails> FindMatchesAtLocus(SearchType searchType, IEnumerable<RegistryCode> registriesToSearch, string locusName, DonorLocusMatchCriteria criteria)
        {
            LocusSearchCriteria repoCriteria = new LocusSearchCriteria
            {
                SearchType = searchType,
                Registries = registriesToSearch,
                HlaNamesToMatchInPositionOne = criteria.HlaNamesToMatchInPositionOne,
                HlaNamesToMatchInPositionTwo = criteria.HlaNamesToMatchInPositionTwo,
            };

            var matches = donorBlobRepository.GetDonorMatchesAtLocus(locusName, repoCriteria)
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

        public SearchableDonor GetDonor(int donorId)
        {
            return donorBlobRepository.GetDonor(donorId);
        }

        public IEnumerable<PotentialHlaMatchRelation> GetMatchesForDonor(int donorId)
        {
            return donorBlobRepository.GetMatchesForDonor(donorId);
        }

        public void InsertDonor(InputDonor donor)
        {
            donorBlobRepository.InsertDonor(donor);
        }

        // TODO:NOVA-929 This will be too many donors
        // Can we stream them in batches with IEnumerable?
        public IEnumerable<RawDonor> AllDonors()
        {
            return donorBlobRepository.AllDonors();
        }

        public void UpdateDonorWithNewHla(InputDonor donor)
        {
            donorBlobRepository.UpdateDonorWithNewHla(donor);
        }
    }
}
