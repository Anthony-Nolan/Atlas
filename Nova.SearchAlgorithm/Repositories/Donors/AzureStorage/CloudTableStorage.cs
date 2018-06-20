using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    public class CloudTableStorage : IDonorDocumentStorage
    {
        private readonly ILogger logger;
        public const string DonorTableReference = "Donors";
        public const string MatchTableReference = "Matches";
        private readonly CloudTable donorTable;
        private readonly CloudTable matchTable;

        public CloudTableStorage(ICloudTableFactory cloudTableFactory, ILogger logger)
        {
            this.logger = logger;
            donorTable = cloudTableFactory.GetTable(DonorTableReference);
            matchTable = cloudTableFactory.GetTable(MatchTableReference);
        }

        public Task<int> HighestDonorId()
        {
            return Task.FromResult(Enum.GetValues(typeof(RegistryCode)).Cast<RegistryCode>()
                .Select(rc =>
                {
                    TableQuery<DonorTableEntity> query = new TableQuery<DonorTableEntity>()
                        .Where(
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, rc.ToString()));

                    // Should be in order of row key ascending (within each partition)
                    return donorTable.ExecuteQuery(query).Reverse().Take(1).Select(d => d.DonorId).FirstOrDefault();
                })
                .Max());
        }

        public Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(Locus locus,
            LocusSearchCriteria criteria)
        {
            var matchesFromPositionOne = GetMatches(locus, criteria.HlaNamesToMatchInPositionOne);
            var matchesFromPositionTwo = GetMatches(locus, criteria.HlaNamesToMatchInPositionTwo);

            return Task.FromResult(matchesFromPositionOne.Select(m => m.ToPotentialHlaMatchRelation(TypePositions.One))
                .Union(matchesFromPositionTwo.Select(m => m.ToPotentialHlaMatchRelation(TypePositions.Two))));
        }

        private IEnumerable<PotentialHlaMatchRelationTableEntity> GetMatches(Locus locus,
            IEnumerable<string> namesToMatch)
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
                matchesQuery = matchesQuery.OrWhere(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal,
                    PotentialHlaMatchRelationTableEntity.GenerateRowKey(locus, name)));
            }

            return matchTable.ExecuteQuery(matchesQuery);
        }

        public Task<DonorResult> GetDonor(int donorId)
        {
            var donorQuery = new TableQuery<DonorTableEntity>().Where(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, donorId.ToString()));
            return Task.Run(() =>
                donorTable.ExecuteQuery(donorQuery).Select(dte => dte.ToDonorResult()).FirstOrDefault());
        }

        public async Task InsertDonor(RawInputDonor donor)
        {
            var insertDonor = TableOperation.Insert(donor.ToTableEntity());
            await donorTable.ExecuteAsync(insertDonor);
        }

        public Task InsertBatchOfDonors(IEnumerable<RawInputDonor> donors)
        {
            var allRegistryCodes = Enum.GetValues(typeof(RegistryCode)).Cast<RegistryCode>();
            return Task.WhenAll(allRegistryCodes
                .Where(rc => donors.Any(d => d.RegistryCode == rc))
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Update the donor itself
            var insertDonor = TableOperation.InsertOrReplace(donor.ToTableEntity());
            await donorTable.ExecuteAsync(insertDonor);
            
            logger.SendTrace("Updated donor", LogLevel.Info, new Dictionary<string, string>
            {
                { "Time", stopwatch.ElapsedMilliseconds.ToString() },
                { "DonorId", donor.DonorId.ToString() }
            });

            await UpdateDonorHlaMatches(donor);
        }

        private async Task UpdateDonorHlaMatches(InputDonor donor)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var matches = AllMatchesForDonor(donor.DonorId).ToList();

            logger.SendTrace("Fetched existing matches", LogLevel.Info, new Dictionary<string, string>
            {
                { "Time", stopwatch.ElapsedMilliseconds.ToString() },
                { "DonorId", donor.DonorId.ToString() },
                { "Matches", matches.Count().ToString() },
            });
            
            stopwatch.Reset();
            stopwatch.Start();
            
            // First delete all the old matches
            var deleteBatchNumber = 0;
            const int batchSize = 100;
            while (matches.Skip(deleteBatchNumber * batchSize).Any())
            {
                var deleteBatchOperation = new TableBatchOperation();
                var matchesToBatch = matches.Skip(deleteBatchNumber * batchSize).Take(batchSize);
                foreach (var match in matchesToBatch)
                {
                    deleteBatchOperation.Delete(match);
                }

                await matchTable.ExecuteBatchAsync(deleteBatchOperation);

                deleteBatchNumber++;
            }
            
            logger.SendTrace("Deleted existing matches", LogLevel.Info, new Dictionary<string, string>
            {
                { "Time", stopwatch.ElapsedMilliseconds.ToString() },
                { "DonorId", donor.DonorId.ToString() },
                { "Matches", matches.Count().ToString() },
            });
            
            stopwatch.Reset();
            stopwatch.Start();

            // Add back the new matches
            var newMatches = donor.MatchingHla
                .FlatMap((locusName, matchingHla1, matchingHla2) =>
                    ConvertToPotentialHlaMatchRelationTableEntities(locusName, matchingHla1, matchingHla2,
                        donor.DonorId))
                .SelectMany(x => x)
                .ToList();
            
            logger.SendTrace("Fetched new matches", LogLevel.Info, new Dictionary<string, string>
            {
                { "Time", stopwatch.ElapsedMilliseconds.ToString() },
                { "DonorId", donor.DonorId.ToString() },
                { "Matches", newMatches.Count().ToString() },
            });
            
            stopwatch.Reset();
            stopwatch.Start();

            var insertBatchNumber = 0;
            while (newMatches.Skip(insertBatchNumber * batchSize).Any())
            {
                var insertBatchOperation = new TableBatchOperation();
                var matchesToBatch = newMatches.Skip(insertBatchNumber * batchSize).Take(batchSize);
                foreach (var match in matchesToBatch)
                {
                    insertBatchOperation.Insert(match);
                }

                await matchTable.ExecuteBatchAsync(insertBatchOperation);

                insertBatchNumber++;
            }
            
            logger.SendTrace("Updated new matches", LogLevel.Info, new Dictionary<string, string>
            {
                { "Time", stopwatch.ElapsedMilliseconds.ToString() },
                { "DonorId", donor.DonorId.ToString() },
                { "Matches", newMatches.Count().ToString() },
            });
        }

        private IEnumerable<PotentialHlaMatchRelationTableEntity> AllMatchesForDonor(int donorId)
        {
            var matchesQuery = new TableQuery<PotentialHlaMatchRelationTableEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                    PotentialHlaMatchRelationTableEntity.GeneratePartitionKey(donorId)));

            return matchTable.ExecuteQuery(matchesQuery);
        }

        private IEnumerable<PotentialHlaMatchRelationTableEntity> ConvertToPotentialHlaMatchRelationTableEntities(
            Locus locusName, ExpandedHla matchingHla1, ExpandedHla matchingHla2, int donorId)
        {
            var list1 = (matchingHla1?.AllMatchingHlaNames() ?? Enumerable.Empty<string>()).ToList();
            var list2 = (matchingHla2?.AllMatchingHlaNames() ?? Enumerable.Empty<string>()).ToList();

            var combinedList = list1.Union(list2).ToList();

            if (!combinedList.Any())
            {
                return new List<PotentialHlaMatchRelationTableEntity>();
            }

            return combinedList.Select(matchName =>
            {
                var typePositions = (TypePositions.None);
                if (list1.Contains(matchName))
                {
                    typePositions |= TypePositions.One;
                }

                if (list2.Contains(matchName))
                {
                    typePositions |= TypePositions.Two;
                }

                return new PotentialHlaMatchRelationTableEntity(locusName, typePositions, matchName, donorId);
            });
        }
    }
}