using Dapper;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Helpers;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public class DonorImportRepository : IDonorImportRepository
    {
        private readonly SearchAlgorithmContext context;
        private readonly IPGroupRepository pGroupRepository;

        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;

        public DonorImportRepository(SearchAlgorithmContext context, IPGroupRepository pGroupRepository)
        {
            this.context = context;
            this.pGroupRepository = pGroupRepository;
        }

        public async Task InsertBatchOfDonors(IEnumerable<InputDonor> donors)
        {
            var rawInputDonors = donors.ToList();

            if (!rawInputDonors.Any())
            {
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("DonorId");
            dt.Columns.Add("DonorType");
            dt.Columns.Add("RegistryCode");
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

            foreach (var donor in rawInputDonors)
            {
                dt.Rows.Add(0,
                    donor.DonorId,
                    (int) donor.DonorType,
                    (int) donor.RegistryCode,
                    donor.HlaNames.A.Position1, donor.HlaNames.A.Position2,
                    donor.HlaNames.B.Position1, donor.HlaNames.B.Position2,
                    donor.HlaNames.C.Position1, donor.HlaNames.C.Position2,
                    donor.HlaNames.Dpb1.Position1, donor.HlaNames.Dpb1.Position2,
                    donor.HlaNames.Dqb1.Position1, donor.HlaNames.Dqb1.Position2,
                    donor.HlaNames.Drb1.Position1, donor.HlaNames.Drb1.Position2);
            }

            using (var sqlBulk = new SqlBulkCopy(connectionString))
            {
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = "Donors";
                await sqlBulk.WriteToServerAsync(dt);
            }
        }

        public async Task InsertDonorWithExpandedHla(InputDonorWithExpandedHla donor)
        {
            await InsertBatchOfDonorsWithExpandedHla(new[] {donor});
        }

        public async Task InsertBatchOfDonorsWithExpandedHla(IEnumerable<InputDonorWithExpandedHla> donors)
        {
            donors = donors.ToList();
            await InsertBatchOfDonors(donors.Select(d => d.ToInputDonor()));
            await AddMatchingPGroupsForExistingDonorBatch(donors);
        }

        // Performance of Entity Framework may not be sufficient to efficiently import large quantities of donors.
        // Consider re-writing this with Dapper if we prove to need to process large donor batches
        public async Task UpdateBatchOfDonorsWithExpandedHla(IEnumerable<InputDonorWithExpandedHla> donors)
        {
            donors = donors.ToList();
            var donorIds = donors.Select(d => d.DonorId);
            var existingDonors = from donor in context.Donors
                join id in donorIds on donor.DonorId equals id
                select donor;
            foreach (var existingDonor in existingDonors.ToList())
            {
                existingDonor.CopyDataFrom(donors.Single(d => d.DonorId == existingDonor.DonorId));
            }

            await ReplaceMatchingGroupsForExistingDonorBatch(donors);
            await context.SaveChangesAsync();
        }

        public async Task AddMatchingPGroupsForExistingDonorBatch(IEnumerable<InputDonorWithExpandedHla> inputDonors)
        {
            await Task.WhenAll(LocusSettings.MatchingOnlyLoci.Select(l => AddMatchingGroupsForExistingDonorBatchAtLocus(inputDonors, l)));
        }

        private async Task ReplaceMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonorWithExpandedHla> inputDonors)
        {
            await Task.WhenAll(LocusSettings.MatchingOnlyLoci.Select(l => ReplaceMatchingGroupsForExistingDonorBatchAtLocus(inputDonors, l)));
        }

        private async Task ReplaceMatchingGroupsForExistingDonorBatchAtLocus(IEnumerable<InputDonorWithExpandedHla> donors, Locus locus)
        {
            donors = donors.ToList();

            var matchingTableName = MatchingTableNameHelper.MatchingTableName(locus);
            var dataTable = CreateDonorDataTableForLocus(donors, locus);

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                var deleteSql = $@"
DELETE FROM {matchingTableName}
WHERE DonorId IN ({string.Join(",", donors.Select(d => d.DonorId))})
";
                // QueryAsync throws exception with 'No columns were selected'
                // https://github.com/StackExchange/Dapper/issues/591
                conn.Query(deleteSql, null, transaction);
                await BulkInsertDataTable(conn, transaction, matchingTableName, dataTable);

                transaction.Commit();
                conn.Close();
            }
        }

        private async Task AddMatchingGroupsForExistingDonorBatchAtLocus(IEnumerable<InputDonorWithExpandedHla> donors, Locus locus)
        {
            var matchingTableName = MatchingTableNameHelper.MatchingTableName(locus);
            var dataTable = CreateDonorDataTableForLocus(donors, locus);

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                await BulkInsertDataTable(conn, transaction, matchingTableName, dataTable);

                transaction.Commit();
                conn.Close();
            }
        }

        private DataTable CreateDonorDataTableForLocus(IEnumerable<InputDonorWithExpandedHla> donors, Locus locus)
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

                    foreach (var pGroup in h.PGroups)
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
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }
    }
}