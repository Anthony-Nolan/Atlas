using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Services;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    public abstract class DonorUpdateRepositoryBase : Repository
    {
        protected readonly IPGroupRepository pGroupRepository;

        protected DonorUpdateRepositoryBase(
            IPGroupRepository pGroupRepository, 
            IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
            this.pGroupRepository = pGroupRepository;
        }
        
        protected async Task InsertBatchOfDonors(IEnumerable<DonorInfo> donors)
        {
            var donorInfos = donors.ToList();

            if (!donorInfos.Any())
            {
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("DonorId");
            dt.Columns.Add("DonorType");
            dt.Columns.Add("A_1");
            dt.Columns.Add("A_2");
            dt.Columns.Add("B_1");
            dt.Columns.Add("B_2");
            dt.Columns.Add("C_1");
            dt.Columns.Add("C_2");
            dt.Columns.Add("DPB1_1");
            dt.Columns.Add("DPB1_2");
            dt.Columns.Add("DQB1_1");
            dt.Columns.Add("DQB1_2");
            dt.Columns.Add("DRB1_1");
            dt.Columns.Add("DRB1_2");

            foreach (var donor in donorInfos)
            {
                dt.Rows.Add(0,
                    donor.DonorId,
                    (int) donor.DonorType,
                    donor.HlaNames.A.Position1, donor.HlaNames.A.Position2,
                    donor.HlaNames.B.Position1, donor.HlaNames.B.Position2,
                    donor.HlaNames.C.Position1, donor.HlaNames.C.Position2,
                    donor.HlaNames.Dpb1.Position1, donor.HlaNames.Dpb1.Position2,
                    donor.HlaNames.Dqb1.Position1, donor.HlaNames.Dqb1.Position2,
                    donor.HlaNames.Drb1.Position1, donor.HlaNames.Drb1.Position2);
            }

            using (var sqlBulk = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString()))
            {
                sqlBulk.BulkCopyTimeout = 3600;
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = "Donors";
                await sqlBulk.WriteToServerAsync(dt);
            }
        }

        protected async Task AddMatchingPGroupsForExistingDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donorInfos)
        {
            await Task.WhenAll(LocusSettings.MatchingOnlyLoci.Select(l => AddMatchingGroupsForExistingDonorBatchAtLocus(donorInfos, l)));
        }

        private async Task AddMatchingGroupsForExistingDonorBatchAtLocus(IEnumerable<DonorInfoWithExpandedHla> donors, Locus locus)
        {
            var matchingTableName = MatchingTableNameHelper.MatchingTableName(locus);
            var dataTable = CreateDonorDataTableForLocus(donors, locus);

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                await BulkInsertDataTable(conn, transaction, matchingTableName, dataTable);

                transaction.Commit();
                conn.Close();
            }
        }

        private DataTable CreateDonorDataTableForLocus(IEnumerable<DonorInfoWithExpandedHla> donors, Locus locus)
        {
            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("DonorId");
            dt.Columns.Add("TypePosition");
            dt.Columns.Add("PGroup_Id");

            foreach (var donor in donors)
            {
                donor.MatchingHla.EachPosition((l, p, h) =>
                {
                    if (h == null || l != locus)
                    {
                        return;
                    }

                    foreach (var pGroup in h.MatchingPGroups)
                    {
                        dt.Rows.Add(0, donor.DonorId, (int) p, pGroupRepository.FindOrCreatePGroup(pGroup));
                    }
                });
            }

            return dt;
        }

        private static async Task BulkInsertDataTable(SqlConnection conn, SqlTransaction transaction, string tableName, DataTable dataTable)
        {
            using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
            {
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = tableName;
                sqlBulk.BulkCopyTimeout = 3600;
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }
    }
}