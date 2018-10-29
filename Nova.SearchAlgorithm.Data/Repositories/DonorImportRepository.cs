using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Matching;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Helpers;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Models.Extensions;
using Nova.SearchAlgorithm.Repositories.Donors;

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

        public async Task InsertBatchOfDonors(IEnumerable<RawInputDonor> donors)
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
                    donor.HlaNames.A_1, donor.HlaNames.A_2,
                    donor.HlaNames.B_1, donor.HlaNames.B_2,
                    donor.HlaNames.C_1, donor.HlaNames.C_2,
                    donor.HlaNames.Dpb1_1, donor.HlaNames.Dpb1_2,
                    donor.HlaNames.Dqb1_1, donor.HlaNames.Dqb1_2,
                    donor.HlaNames.Drb1_1, donor.HlaNames.Drb1_2);
            }

            using (var sqlBulk = new SqlBulkCopy(connectionString))
            {
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = "Donors";
                await sqlBulk.WriteToServerAsync(dt);
            }
        }

        public async Task AddOrUpdateDonorWithHla(InputDonor donor)
        {
            var result = await context.Donors.FirstOrDefaultAsync(d => d.DonorId == donor.DonorId);
            if (result == null)
            {
                context.Donors.Add(donor.ToDonorEntity());
            }
            else
            {
                result.CopyRawHlaFrom(donor);
            }

            await RefreshMatchingGroupsForExistingDonorBatch(new List<InputDonor> {donor});

            await context.SaveChangesAsync();
        }

        public async Task RefreshMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            await Task.WhenAll(LocusHelpers.AllLoci().Select(l => RefreshMatchingGroupsForExistingDonorBatchAtLocus(inputDonors, l)));
        }

        public void SetupForHlaRefresh()
        {
            // Do nothing
        }

        private async Task RefreshMatchingGroupsForExistingDonorBatchAtLocus(IEnumerable<InputDonor> donors, Locus locus)
        {
            if (locus == Locus.Dpb1)
            {
                return;
            }

            var tableName = MatchingTableName(locus);

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                var dataTableGenerationTask = Task.Run(() =>
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
                });

                var dataTable = await dataTableGenerationTask;

                using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
                {
                    sqlBulk.BatchSize = 10000;
                    sqlBulk.DestinationTableName = tableName;
                    sqlBulk.WriteToServer(dataTable);
                }

                transaction.Commit();
                conn.Close();
            }
        }

        private static string MatchingTableName(Locus locus)
        {
            return "MatchingHlaAt" + locus;
        }
    }
}