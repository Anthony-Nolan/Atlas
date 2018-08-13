using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData
{
    public static class TestDataService
    {
        public static void SetupTestData()
        {
            TestDataRepository.SetupDatabase();
            TestDataRepository.AddTestDonors(MetaDonorRepository.MetaDonors.SelectMany(md => md.GetDatabaseDonors()));
        }
    }
}