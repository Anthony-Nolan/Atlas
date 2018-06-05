using System;
using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    public class CloudTableStorage : IDonorDocumentStorage
    {
        public const string DonorTableReference = "Donors";
        public const string MatchTableReference = "Matches";
        private readonly CloudTable donorTable;
        private readonly CloudTable matchTable;
        private readonly IMapper mapper;

        public CloudTableStorage(IMapper mapper, ICloudTableFactory cloudTableFactory)
        {
            donorTable = cloudTableFactory.GetTable(DonorTableReference);
            matchTable = cloudTableFactory.GetTable(MatchTableReference);
            this.mapper = mapper;
        }

        public Task<int> HighestDonorId()
        {
            return Task.FromResult(Enum.GetValues(typeof(RegistryCode)).Cast<RegistryCode>()
                .Select(rc =>
                    {
                        TableQuery<DonorTableEntity> query = new TableQuery<DonorTableEntity>()
                            .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, rc.ToString()));

                        // Should be in order of row key (within each partition)
                        return donorTable.ExecuteQuery(query).Take(1).Select(d => d.DonorId).FirstOrDefault();
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
            return Task.FromResult(donorTable.ExecuteQuery(donorQuery).Select(dte => dte.ToRawDonor(mapper)).FirstOrDefault());
        }

        public Task<IEnumerable<PotentialHlaMatchRelation>> GetMatchesForDonor(int donorId)
        {
            return Task.FromResult(AllMatchesForDonor(donorId).Select(m => m.ToPotentialHlaMatchRelation(0)));
        }

        public async Task InsertDonor(InputDonor donor)
        {
            var insertDonor = TableOperation.InsertOrReplace(donor.ToTableEntity(mapper));
            await donorTable.ExecuteAsync(insertDonor);

            UpdateDonorHlaMatches(donor);
        }

        // TODO:NOVA-939 This will be too many donors
        // Can we stream them in batches with IEnumerable?
        public Task<IEnumerable<DonorResult>> AllDonors()
        {
            var query = new TableQuery<DonorTableEntity>();
            return Task.FromResult(donorTable.ExecuteQuery(query).Select(dte => dte.ToRawDonor(mapper)));
        }

        public async Task UpdateDonorWithNewHla(InputDonor donor)
        {
            // Update the donor itself
            var insertDonor = TableOperation.InsertOrReplace(donor.ToTableEntity(mapper));
            await donorTable.ExecuteAsync(insertDonor);
            UpdateDonorHlaMatches(donor);
        }

        private void UpdateDonorHlaMatches(InputDonor donor)
        {
            // First delete all the old matches
            var matches = AllMatchesForDonor(donor.DonorId);
            foreach (var match in matches)
            {
                matchTable.Execute(TableOperation.Delete(match));
            }

            // Add back the new matches
            donor.MatchingHla.EachLocus((locusName, matchingHla1, matchingHla2) => InsertLocusMatch(locusName, matchingHla1, matchingHla2, donor.DonorId));
        }

        private IEnumerable<PotentialHlaMatchRelationTableEntity> AllMatchesForDonor(int donorId)
        { 
            var matchesQuery = new TableQuery<PotentialHlaMatchRelationTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, donorId.ToString()));

            return matchTable.ExecuteQuery(matchesQuery);
        }

        private void InsertLocusMatch(Locus locusName, ExpandedHla matchingHla1, ExpandedHla matchingHla2, int donorId)
        {
            if (matchingHla1 == null)
            {
                return;
            }

            var list1 = matchingHla1.AllMatchingHlaNames().ToList();
            var list2 = matchingHla2.AllMatchingHlaNames().ToList();

            foreach (string matchName in list1.Union(list2))
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
                var insertMatch = TableOperation.InsertOrMerge(new PotentialHlaMatchRelationTableEntity(locusName, typePositions, matchName, donorId));
                matchTable.Execute(insertMatch);
            }
        }
    }
}
