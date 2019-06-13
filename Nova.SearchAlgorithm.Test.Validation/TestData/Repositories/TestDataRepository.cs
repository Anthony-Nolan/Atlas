using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Persistent;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public interface ITestDataRepository
    {
        void SetupPersistentDatabase();
        void SetupDatabase();
        void AddTestDonors(IEnumerable<Donor> donors);
        IEnumerable<Donor> GetDonors(IEnumerable<int> donorIds);
    }

    /// <summary>
    /// Repository layer for interacting with test SQL database
    /// </summary>
    public class TestDataRepository : ITestDataRepository
    {
        private readonly SearchAlgorithmContext context;
        private readonly SearchAlgorithmPersistentContext persistentContext;

        public TestDataRepository(SearchAlgorithmContext context, SearchAlgorithmPersistentContext persistentContext)
        {
            this.context = context;
            this.persistentContext = persistentContext;
        }

        public void SetupPersistentDatabase()
        {
            persistentContext.Database.Migrate();
        }

        public void SetupDatabase()
        {
            context.Database.Migrate();
            // Ensure we have fresh data on each run. Done in setup rather than teardown to avoid data issues if the test suites are terminated early
            RemoveTestData();
        }

        public void AddTestDonors(IEnumerable<Donor> donors)
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

        public IEnumerable<Donor> GetDonors(IEnumerable<int> donorIds)
        {
            var donors = from d in context.Donors
                join id in donorIds
                    on d.DonorId equals id
                select d;
            return donors.ToList();
        }

        private void RemoveTestData()
        {
            context.Donors.RemoveRange(context.Donors);
            context.MatchingHlaAtA.RemoveRange(context.MatchingHlaAtA);
            context.MatchingHlaAtB.RemoveRange(context.MatchingHlaAtB);
            context.MatchingHlaAtC.RemoveRange(context.MatchingHlaAtC);
            context.MatchingHlaAtDrb1.RemoveRange(context.MatchingHlaAtDrb1);
            context.MatchingHlaAtDqb1.RemoveRange(context.MatchingHlaAtDqb1);
            context.SaveChanges();
        }
    }
}