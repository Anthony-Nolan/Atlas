using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public static class TestDataRepository
    {
        public static void SetupDatabase()
        {
            using (var context = new SearchAlgorithmContext())
            {
                context.Database.CreateIfNotExists();
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
                context.MatchingHlaAtA.RemoveRange(context.MatchingHlaAtA);
                context.MatchingHlaAtB.RemoveRange(context.MatchingHlaAtB);
                context.MatchingHlaAtC.RemoveRange(context.MatchingHlaAtC);
                context.MatchingHlaAtDrb1.RemoveRange(context.MatchingHlaAtDrb1);
                context.MatchingHlaAtDqb1.RemoveRange(context.MatchingHlaAtDqb1);
                context.SaveChanges();
            }
        }
    }
}