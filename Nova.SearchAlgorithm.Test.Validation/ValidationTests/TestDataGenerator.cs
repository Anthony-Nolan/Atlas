using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests
{
    public static class TestDataGenerator
    {
        public static void SetupDatabase()
        {
            using (var context = new SearchAlgorithmContext())
            {
                context.Database.CreateIfNotExists();
            }
        }

        public static void AddTestDonors()
        {
            using (var context = new SearchAlgorithmContext())
            {
                var donors = new List<Donor>
                {
                    new Donor
                    {
                        DonorId = 1,
                        DonorType = DonorType.Adult,
                        RegistryCode = RegistryCode.AN,
                        A_1 = "01:01",
                        A_2 = "11:02",
                        B_1 = "07:02",
                        B_2 = "08:41",
                        DRB1_1 = "15:09",
                        DRB1_2 = "12:02",
                        C_1 = "04:01",
                        C_2 = "15:02",
                        DQB1_1 = "05:01",
                        DQB1_2 = "06:01",
                    },
                    new Donor
                    {
                        DonorId = 2,
                        DonorType = DonorType.Cord,
                        RegistryCode = RegistryCode.AN,
                        A_1 = "01:01",
                        A_2 = "11:02",
                        B_1 = "07:02",
                        B_2 = "08:41",
                        DRB1_1 = "15:09",
                        DRB1_2 = "12:02",
                        C_1 = "04:01",
                        C_2 = "15:02",
                        DQB1_1 = "05:01",
                        DQB1_2 = "06:01",
                    },
                };

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