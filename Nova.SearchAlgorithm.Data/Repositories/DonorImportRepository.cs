using Dapper;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Helpers;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Config;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Services;
// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public class DonorImportRepository : DonorUpdateRepositoryBase, IDonorImportRepository
    {
        private const string MatchingHlaTable_IndexName_PGroupIdAndDonorId = "IX_PGroup_Id_DonorId__TypePosition";
        private const string MatchingHlaTable_IndexName_DonorId = "IX_DonorId__PGroup_Id_TypePosition";
        
        public DonorImportRepository(IPGroupRepository pGroupRepository, IConnectionStringProvider connectionStringProvider) : base(pGroupRepository, connectionStringProvider)
        {
        }

        public async Task FullHlaRefreshSetUp()
        {
            var indexRemovalSql = $@"
DROP INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtA;
DROP INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtB;
DROP INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtC;
DROP INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtDrb1;
DROP INDEX {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtDqb1;
DROP INDEX {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtA;
DROP INDEX {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtB;
DROP INDEX {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtC;
DROP INDEX {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtDrb1;
DROP INDEX {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtDqb1;
";
            using (var conn = new SqlConnection(connectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(indexRemovalSql);
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
            using (var conn = new SqlConnection(connectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(indexAdditionSql);
            }
        }

        public new async Task InsertBatchOfDonors(IEnumerable<InputDonor> donors)
        {
            await base.InsertBatchOfDonors(donors);
        }

        public new async Task AddMatchingPGroupsForExistingDonorBatch(IEnumerable<InputDonorWithExpandedHla> donors)
        {
            await base.AddMatchingPGroupsForExistingDonorBatch(donors);
        }
    }
}