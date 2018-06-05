using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage
{
    public class CosmosStorage : IDonorDocumentStorage
    {
        private DocumentDBRepository<PotentialHlaMatchRelationCosmosDocument> matchRepo = new DocumentDBRepository<PotentialHlaMatchRelationCosmosDocument>();
        private DocumentDBRepository<DonorCosmosDocument> donorRepo = new DocumentDBRepository<DonorCosmosDocument>();

        public int HighestDonorId()
        {
            //TODO:NOVA-1295 implement
            return 0;
        }

        public async Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(Locus locus, LocusSearchCriteria criteria)
        {
            var result = await Task.WhenAll(
                GetMatches(locus, criteria.HlaNamesToMatchInPositionOne),
                GetMatches(locus, criteria.HlaNamesToMatchInPositionTwo));

            var matchesFromPositionOne = result[0].Select(m => m.ToPotentialHlaMatchRelation(TypePositions.One));
            var matchesFromPositionTwo = result[1].Select(m => m.ToPotentialHlaMatchRelation(TypePositions.Two));

            return matchesFromPositionOne.Union(matchesFromPositionTwo);
        }

        private async Task<IEnumerable<PotentialHlaMatchRelationCosmosDocument>> GetMatches(Locus locus, IEnumerable<string> namesToMatch)
        {
            // Enumerate once
            var namesToMatchList = namesToMatch.ToList();

            if (!namesToMatchList.Any())
            {
                return Enumerable.Empty<PotentialHlaMatchRelationCosmosDocument>();
            }

            return await matchRepo.GetItemsAsync(p =>
                p.Locus == locus && namesToMatchList.Contains(p.Name));
        }

        public async Task<DonorResult> GetDonor(int donorId)
        {
            return (await donorRepo.GetItemAsync(donorId.ToString())).ToDonorResult();
        }

        public async Task<IEnumerable<PotentialHlaMatchRelation>> GetMatchesForDonor(int donorId)
        {
            return (await AllMatchesForDonor(donorId)).Select(m => m.ToPotentialHlaMatchRelation(0));
        }

        public async Task InsertDonor(InputDonor donor)
        {
            await donorRepo.CreateItemAsync(DonorCosmosDocument.FromInputDonor(donor));

            await UpdateDonorHlaMatches(donor);
        }

        // TODO:NOVA-939 This will be too many donors
        // Can we stream them in batches with IEnumerable?
        public async Task<IEnumerable<DonorResult>> AllDonors()
        {
            return (await donorRepo.GetItemsAsync(d => true)).Select(d => d.ToDonorResult());
        }

        private Task<IEnumerable<PotentialHlaMatchRelationCosmosDocument>> AllMatchesForDonor(int donorId)
        {
            return matchRepo.GetItemsAsync(p => p.DonorId == donorId);
        }

        public async Task UpdateDonorWithNewHla(InputDonor donor)
        {
            // Update the donor itself
            await donorRepo.UpdateItemAsync(donor.DonorId.ToString(), DonorCosmosDocument.FromInputDonor(donor));
            await UpdateDonorHlaMatches(donor);
        }

        private async Task UpdateDonorHlaMatches(InputDonor donor)
        {
            // First delete all the old matches
            var matches = await AllMatchesForDonor(donor.DonorId);
            await Task.WhenAll(matches.Select(m =>
                matchRepo.DeleteItemAsync(m.Id)));

            // Add back the new matches
            await donor.MatchingHla.WhenAllLoci((locusName, matchingHla1, matchingHla2) => InsertLocusMatch(locusName, matchingHla1, matchingHla2, donor.DonorId));
        }

        private Task InsertLocusMatch(Locus locus, ExpandedHla matchingHla1, ExpandedHla matchingHla2, int donorId)
        {
            if (matchingHla1 == null)
            {
                return Task.CompletedTask;
            }

            var list1 = matchingHla1.AllMatchingHlaNames().ToList();
            var list2 = matchingHla2.AllMatchingHlaNames().ToList();

            return Task.WhenAll(list1.Union(list2).Select(matchName =>
            {
                TypePositions typePositions = (TypePositions.None);
                if (list1.Contains(matchName))
                {
                    typePositions |= TypePositions.One;
                }

                if (list2.Contains(matchName))
                {
                    typePositions |= TypePositions.Two;
                }
                
                return matchRepo.CreateItemAsync(
                    new PotentialHlaMatchRelationCosmosDocument
                    {
                        Id = PotentialHlaMatchRelationCosmosDocument.GenerateId(locus, donorId, matchName),
                        DonorId = donorId,
                        Locus = locus,
                        MatchingTypePositions = typePositions,
                        Name = matchName
                    });
            }));
        }
    }
}
