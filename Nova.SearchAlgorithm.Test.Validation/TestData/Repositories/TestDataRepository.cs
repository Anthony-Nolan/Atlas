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
    }
}