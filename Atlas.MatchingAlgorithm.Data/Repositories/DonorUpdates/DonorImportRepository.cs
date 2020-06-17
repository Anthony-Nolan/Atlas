using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    public class DonorImportRepository : DonorUpdateRepositoryBase, IDonorImportRepository
    {
        private const string MatchingHlaTable_IndexName_PGroupIdAndDonorId = "IX_PGroup_Id_DonorId__TypePosition";
        private const string MatchingHlaTable_IndexName_DonorId = "IX_DonorId__PGroup_Id_TypePosition";

        private const string DropAllDonorsSql = @"
TRUNCATE TABLE [Donors]
";
        
        private const string DropAllPreProcessedDonorHlaSql = @"
TRUNCATE TABLE [MatchingHlaAtA]
TRUNCATE TABLE [MatchingHlaAtB]
TRUNCATE TABLE [MatchingHlaAtC]
TRUNCATE TABLE [MatchingHlaAtDrb1]
TRUNCATE TABLE [MatchingHlaAtDqb1]
";

        public DonorImportRepository(
            IPGroupRepository pGroupRepository,
            IConnectionStringProvider connectionStringProvider) : base(pGroupRepository, connectionStringProvider)
        {
        }

        public async Task FullHlaRefreshSetUp()
        {
            var indexRemovalSql = $@"
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtA;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtB;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtC;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtDrb1;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtDqb1;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtA;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtB;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtC;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtDrb1;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtDqb1;
";
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(indexRemovalSql, commandTimeout: 600);
            }
        }

        public async Task FullHlaRefreshTearDown()
        {
            var indexAdditionSql = $@"
CREATE INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId}
ON MatchingHlaAtA (PGroup_Id, DonorId)
INCLUDE (TypePosition)

CREATE INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId}
ON MatchingHlaAtB (PGroup_Id, DonorId)
INCLUDE (TypePosition)

CREATE INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId}
ON MatchingHlaAtC (PGroup_Id, DonorId)
INCLUDE (TypePosition)

CREATE INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId}
ON MatchingHlaAtDrb1 (PGroup_Id, DonorId)
INCLUDE (TypePosition)

CREATE INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId}
ON MatchingHlaAtDqb1 (PGroup_Id, DonorId)
INCLUDE (TypePosition)


CREATE INDEX {MatchingHlaTable_IndexName_DonorId}
ON MatchingHlaAtA (DonorId)
INCLUDE (TypePosition, PGroup_Id)

CREATE INDEX {MatchingHlaTable_IndexName_DonorId}
ON MatchingHlaAtB (DonorId)
INCLUDE (TypePosition, PGroup_Id)

CREATE INDEX {MatchingHlaTable_IndexName_DonorId}
ON MatchingHlaAtC (DonorId)
INCLUDE (TypePosition, PGroup_Id)

CREATE INDEX {MatchingHlaTable_IndexName_DonorId}
ON MatchingHlaAtDrb1 (DonorId)
INCLUDE (TypePosition, PGroup_Id)

CREATE INDEX {MatchingHlaTable_IndexName_DonorId}
ON MatchingHlaAtDqb1 (DonorId)
INCLUDE (TypePosition, PGroup_Id)
";
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(indexAdditionSql, commandTimeout: 10800);
            }
        }

        public async Task RemoveAllDonorInformation()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(DropAllPreProcessedDonorHlaSql, commandTimeout: 300);
                await conn.ExecuteAsync(DropAllDonorsSql, commandTimeout: 300);
            }
        }

        /// <inheritdoc />
        public async Task RemoveAllProcessedDonorHla()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(DropAllPreProcessedDonorHlaSql, commandTimeout: 300);
            }
        }

        public new async Task InsertBatchOfDonors(IEnumerable<DonorInfo> donors)
        {
            await base.InsertBatchOfDonors(donors);
        }

        public new async Task AddMatchingPGroupsForExistingDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donors)
        {
            await base.AddMatchingPGroupsForExistingDonorBatch(donors);
        }

        public async Task RemovePGroupsForDonorBatch(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtA");
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtB");
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtC");
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtDqb1");
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtDrb1");
        }

        private async Task RemovePGroupsForDonorBatchAtLocus(IEnumerable<int> donorIds, string locusTableName)
        {
            var removalSql = $@"
DELETE FROM {locusTableName}
WHERE DonorId IN ({string.Join(",", donorIds)});
";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(removalSql, commandTimeout: 600);
            }
        }
    }
}