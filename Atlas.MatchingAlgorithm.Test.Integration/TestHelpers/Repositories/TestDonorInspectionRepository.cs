using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories
{
    public class DonorWithLog
    {
        public Donor Donor { get; set; }
        public DonorManagementLog Log { get; set; }
    }
    public class TestDonorInspectionRepository : DonorInspectionRepository
    {
        public TestDonorInspectionRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public int GetDonorCount()
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM Donors", commandTimeout: 1);
            }
        }

        public List<int> GetAllDonorIds()
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return conn.Query<int>($"SELECT DonorId FROM Donors", commandTimeout: 2).ToList();
            }
        }

        public Dictionary<int, DonorWithLog> GetAllDonorsWithLogs()
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var donorLogsDictionary = conn.Query<DonorManagementLog>($"SELECT * FROM DonorManagementLogs", commandTimeout: 5).ToDictionary(d => d.DonorId);
                var donors = conn.Query<Donor>($"SELECT * FROM Donors", commandTimeout: 5);

                return donors.ToDictionary(
                    d => d.DonorId,
                    d => new DonorWithLog
                    {
                        Donor = d,
                        Log = donorLogsDictionary[d.DonorId]
                    });
            }
        }
    }
}