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
    public class BlobDonorMatchRepository : IDonorMatchRepository
    {
        private readonly IDonorBlobRepository donorBlobRepository;

        public BlobDonorMatchRepository(IDonorBlobRepository donorBlobRepository)
        {
            this.donorBlobRepository = donorBlobRepository;
        }

        public IEnumerable<PotentialMatch> Search(DonorMatchCriteria matchRequest)
        {
            // TODO:NOVA-931 extend beyond locus A
            LocusSearchCriteria criteriaA = new LocusSearchCriteria
            {
                SearchType = matchRequest.SearchType,
                Registries = matchRequest.RegistriesToSearch,
                HlaNamesToMatchInPositionOne = matchRequest.LocusMismatchA.HlaNamesToMatchInPositionOne,
                HlaNamesToMatchInPositionTwo = matchRequest.LocusMismatchA.HlaNamesToMatchInPositionTwo,
            };

            var matches = donorBlobRepository.GetDonorMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, "A", criteriaA)
                .GroupBy(m => m.DonorId);

            if (matchRequest.LocusMismatchA.MismatchCount == 0)
            {
                matches = matches.Where(g => DirectMatch(g) || CrossMatch(g));
            }

            return matches.Select(DonorMatchFromGroup);
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

        private PotentialMatch DonorMatchFromGroup(IGrouping<int, PotentialHlaMatchRelation> group)
        {
            var donor = donorBlobRepository.GetDonor(group.Key).ToApiDonorMatch();

            // TODO:NOVA-931 extend beyond locus A
            donor.TotalMatchCount = DirectMatch(group) || CrossMatch(group) ? 2 : 1;

            return donor;
        }

        public SearchableDonor GetDonor(int donorId)
        {
            return donorBlobRepository.GetDonor(donorId);
        }

        public IEnumerable<PotentialHlaMatchRelation> GetMatchesForDonor(int donorId)
        {
            return donorBlobRepository.GetMatchesForDonor(donorId);
        }

        public void InsertDonor(SearchableDonor donor)
        {
            donorBlobRepository.InsertDonor(donor);
        }

        // TODO:NOVA-929 This will be too many donors
        // Can we stream them in batches with IEnumerable?
        public IEnumerable<SearchableDonor> AllDonors()
        {
            return donorBlobRepository.AllDonors();
        }

        public void UpdateDonorWithNewHla(SearchableDonor donor)
        {
            donorBlobRepository.UpdateDonorWithNewHla(donor);
        }
    }
}
