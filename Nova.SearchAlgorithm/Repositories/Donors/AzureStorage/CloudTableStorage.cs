using System;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    public class CloudTableStorage : IDonorDocumentStorage
    {
        public const string DonorTableReference = "Donors";
        public const string MatchTableReference = "Matches";
        private readonly CloudTable donorTable;
        private readonly CloudTable matchTable;

        public CloudTableStorage(ICloudTableFactory cloudTableFactory)
        {
            donorTable = cloudTableFactory.GetTable(DonorTableReference);
            matchTable = cloudTableFactory.GetTable(MatchTableReference);
        }

        public Task<int> HighestDonorId()
        {
            return Task.FromResult(Enum.GetValues(typeof(RegistryCode)).Cast<RegistryCode>()
                .Select(rc =>
                    {
                        TableQuery<DonorTableEntity> query = new TableQuery<DonorTableEntity>()
                            .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, rc.ToString()));

                        // Should be in order of row key ascending (within each partition)
                        return donorTable.ExecuteQuery(query).Reverse().Take(1).Select(d => d.DonorId).FirstOrDefault();
                    })
                .Max());
        }

        public Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(Locus locus, LocusSearchCriteria criteria)
        {
            var matchesFromPositionOne = GetMatches(locus, criteria.HlaNamesToMatchInPositionOne);
            var matchesFromPositionTwo = GetMatches(locus, criteria.HlaNamesToMatchInPositionTwo);

            return Task.FromResult(matchesFromPositionOne.Select(m => m.ToPotentialHlaMatchRelation(TypePositions.One)).Union(matchesFromPositionTwo.Select(m => m.ToPotentialHlaMatchRelation(TypePositions.Two))));
        }

        private IEnumerable<PotentialHlaMatchRelationTableEntity> GetMatches(Locus locus, IEnumerable<string> namesToMatch)
        {
            // Enumerate once
            var namesToMatchList = namesToMatch.ToList();

            if (!namesToMatchList.Any())
            {
                return Enumerable.Empty<PotentialHlaMatchRelationTableEntity>();
            }

            var matchesQuery = new TableQuery<PotentialHlaMatchRelationTableEntity>();
            foreach (string name in namesToMatchList)
            {
                matchesQuery = matchesQuery.OrWhere(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PotentialHlaMatchRelationTableEntity.GeneratePartitionKey(locus, name)));
            }

            return matchTable.ExecuteQuery(matchesQuery);
        }

        public Task<DonorResult> GetDonor(int donorId)
        {
            var donorQuery = new TableQuery<DonorTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, donorId.ToString()));
            return Task.Run(() => donorTable.ExecuteQuery(donorQuery).Select(dte => dte.ToDonorResult()).FirstOrDefault());
        }

        public async Task InsertDonor(RawInputDonor donor)
        {
            var insertDonor = TableOperation.Insert(donor.ToTableEntity());
            await donorTable.ExecuteAsync(insertDonor);
        }

        public Task InsertBatchOfDonors(IEnumerable<RawInputDonor> donors)
        {
            return Task.WhenAll(Enum.GetValues(typeof(RegistryCode)).Cast<RegistryCode>()
                .Select(rc =>
                {
                    var batchOperation = new TableBatchOperation();
                    foreach (var donor in donors.Where(d => d.RegistryCode == rc))
                    {
                        batchOperation.Insert(donor.ToTableEntity());
                    }
                    return donorTable.ExecuteBatchAsync(batchOperation);
                }));
        }

        public IBatchQueryAsync<DonorResult> AllDonors()
        {
            var query = new TableQuery<DonorTableEntity>();
            return new CloudTableDonorBatchQueryAsync(query, donorTable);
        }

        public async Task RefreshMatchingGroupsForExistingDonor(InputDonor donor)
        {
            // Update the donor itself
            var insertDonor = TableOperation.InsertOrReplace(donor.ToTableEntity());
            await donorTable.ExecuteAsync(insertDonor);

            await UpdateDonorHlaMatches(donor);
        }

        private async Task UpdateDonorHlaMatches(InputDonor donor)
        {
            // First delete all the old matches
            var matches = AllMatchesForDonor(donor.DonorId).ToList();
            // We can't batch delete if all the partition keys are different
            await Task.WhenAll(matches.Select(m => matchTable.ExecuteAsync(TableOperation.Delete(m))));
            
            // Add back the new matches
            await donor.MatchingHla.WhenAllLoci((locusName, matchingHla1, matchingHla2) => InsertLocusMatch(locusName, matchingHla1, matchingHla2, donor.DonorId));
        }

        private IEnumerable<PotentialHlaMatchRelationTableEntity> AllMatchesForDonor(int donorId)
        { 
            var matchesQuery = new TableQuery<PotentialHlaMatchRelationTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, donorId.ToString()));

            return matchTable.ExecuteQuery(matchesQuery);
        }

        private Task InsertLocusMatch(Locus locusName, ExpandedHla matchingHla1, ExpandedHla matchingHla2, int donorId)
        {
            var list1 = (matchingHla1?.AllMatchingHlaNames() ?? Enumerable.Empty<string>()).ToList();
            var list2 = (matchingHla2?.AllMatchingHlaNames() ?? Enumerable.Empty<string>()).ToList();

            var combinedList = list1.Union(list2).ToList();

            if (!combinedList.Any())
            {
                return Task.CompletedTask;
            }

            // Can't batch the inserts since the partition names differ
            return Task.WhenAll(combinedList.Select(matchName =>
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

                var insertMatch =
                    TableOperation.InsertOrMerge(
                        new PotentialHlaMatchRelationTableEntity(locusName, typePositions, matchName, donorId));

                return matchTable.ExecuteAsync(insertMatch);
            }));
        }
    }
}
