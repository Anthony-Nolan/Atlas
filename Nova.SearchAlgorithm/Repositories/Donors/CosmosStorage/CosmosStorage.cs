using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Exceptions;

namespace Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage
{
    public class CosmosStorage : IDonorDocumentStorage
    {
        private readonly string DatabaseId = ConfigurationManager.AppSettings["cosmos.database"];
        private readonly DocumentClient client;

        public CosmosStorage()
        {
            client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["cosmos.endpoint"]), ConfigurationManager.AppSettings["cosmos.authKey"]);
            Task.Run(() => client.CreateDatabaseIfNotExistsAsync()).Wait();
            Task.Run(() => client.CreateCollectionIfNotExistsAsync<DonorCosmosDocument>()).Wait();
            Task.Run(() => client.CreateCollectionIfNotExistsAsync<PotentialHlaMatchRelationCosmosDocument>()).Wait();
        }

        public async Task<int> HighestDonorId()
        {
            var query = client.CreateDocumentQuery<DonorCosmosDocument>(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId, client.CollectionId<DonorCosmosDocument>()),
                    new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                .OrderByDescending(d => d.DuplicateIdForOrdering)
                .Take(1)
                .AsDocumentQuery();

            if (!query.HasMoreResults)
            {
                return 0;
            }

            var firstResults = await query.ExecuteNextAsync<DonorCosmosDocument>();

            // If there are no donors in the database yet, return 0.
            return firstResults.Any() ? firstResults.First().DuplicateIdForOrdering : 0;
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

            return await client.GetItemsAsync<PotentialHlaMatchRelationCosmosDocument>(p =>
                p.Locus == locus && namesToMatchList.Contains(p.Name));
        }

        public async Task<DonorResult> GetDonor(int donorId)
        {
            return (await client.GetItemAsync<DonorCosmosDocument>(donorId.ToString())).ToDonorResult();
        }

        public async Task InsertDonor(RawInputDonor donor)
        {
            await client.CreateItemAsync(DonorCosmosDocument.FromRawInputDonor(donor));
        }

        public Task InsertBatchOfDonors(IEnumerable<RawInputDonor> donors)
        {
            throw new NotImplementedException();
        }

        public void SetupForHlaRefresh()
        {
            // Do Nothing
        }

        public IBatchQueryAsync<DonorResult> AllDonors()
        {
            IDocumentQuery<DonorCosmosDocument> query = client.CreateDocumentQuery<DonorCosmosDocument>(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId, client.CollectionId<DonorCosmosDocument>()),
                    new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true,  })
                .AsDocumentQuery();

            return new CosmosDonorResultBatchQueryAsync(query);
        }

        private Task<IEnumerable<PotentialHlaMatchRelationCosmosDocument>> AllMatchesForDonor(int donorId)
        {
            return client.GetItemsAsync<PotentialHlaMatchRelationCosmosDocument>(p => p.DonorId == donorId);
        }

        public async Task RefreshMatchingGroupsForExistingDonor(InputDonor donor)
        {
            // Update the donor itself
            await client.UpdateItemAsync(donor.DonorId.ToString(), DonorCosmosDocument.FromInputDonor(donor));
            await UpdateDonorHlaMatches(donor);
        }

        private async Task UpdateDonorHlaMatches(InputDonor donor)
        {
            // First delete all the old matches
            var matches = await AllMatchesForDonor(donor.DonorId);
            await Task.WhenAll(matches.Select(m =>
                client.DeleteItemAsync<PotentialHlaMatchRelationCosmosDocument>(m.Id)));

            // Add back the new matches
            await donor.MatchingHla.WhenAllLoci((locusName, matchingHla1, matchingHla2) => InsertLocusMatch(locusName, matchingHla1, matchingHla2, donor));
        }

        private Task InsertLocusMatch(Locus locus, ExpandedHla matchingHla1, ExpandedHla matchingHla2, InputDonor donor)
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
                
                return client.CreateItemAsync(
                    new PotentialHlaMatchRelationCosmosDocument
                    {
                        DonorId = donor.DonorId,
                        Locus = locus,
                        MatchingTypePositions = typePositions,
                        Donor = DonorCosmosDocument.FromInputDonor(donor),
                        Name = matchName
                    });
            }));
        }
    }
}
