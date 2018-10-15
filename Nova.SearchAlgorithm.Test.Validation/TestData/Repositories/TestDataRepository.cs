using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    /// <summary>
    /// Repository layer for interacting with test SQL database
    /// </summary>
    public static class TestDataRepository
    {
        public static void SetupDatabase()
        {
            using (var context = new SearchAlgorithmContext())
            {
                context.Database.CreateIfNotExists();
                var config = new Data.Migrations.Configuration();
                var migrator = new DbMigrator(config);
                migrator.Update();
                RemoveTestData();
            }
        }

        public static void AddTestDonors(IEnumerable<Donor> donors)
        {
            using (var context = new SearchAlgorithmContext())
            {
                foreach (var donor in donors)
                {
                    if (!context.Donors.Any(d => d.DonorId == donor.DonorId))
                    {
                        context.Donors.Add(donor);
                    }
                }

                context.SaveChanges();
            }
        }

        private static void RemoveTestData()
        {
            using (var context = new SearchAlgorithmContext())
            {
                context.Donors.RemoveRange(context.Donors);
                context.MatchingHlaAtA.SqlQuery("TRUNCATE TABLE MatchingHlaAtA");
                context.MatchingHlaAtA.SqlQuery("TRUNCATE TABLE MatchingHlaAtB");
                context.MatchingHlaAtA.SqlQuery("TRUNCATE TABLE MatchingHlaAtC");
                context.MatchingHlaAtA.SqlQuery("TRUNCATE TABLE MatchingHlaAtDrb1");
                context.MatchingHlaAtA.SqlQuery("TRUNCATE TABLE MatchingHlaAtDqb1");
                context.SaveChanges();
            }
        }
    }
}