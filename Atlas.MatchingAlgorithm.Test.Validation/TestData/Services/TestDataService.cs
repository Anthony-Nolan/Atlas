using System.Linq;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    public interface ITestDataService
    {
        void SetupTestData();
    }

    public class TestDataService : ITestDataService
    {
        private readonly IMetaDonorRepository metaDonorRepository;
        private readonly ITestDataRepository testDataRepository;

        public TestDataService(IMetaDonorRepository metaDonorRepository, ITestDataRepository testDataRepository)
        {
            this.metaDonorRepository = metaDonorRepository;
            this.testDataRepository = testDataRepository;
        }

        public void SetupTestData()
        {
            // This must happen before the transient database, as the databases are shared and both contain a `Donors` table.
            // This should be allowed, but EF refuses to run migrations adding `Donors.Donors` if `dbo.Donors` already exists.
            // Adding `Donors.Donors` THEN `dbo.Donors` is apparently fine.
            testDataRepository.SetUpDonorDatabase();
            
            testDataRepository.SetupPersistentMatchingDatabase();
            testDataRepository.SetupTransientMatchingDatabase();
            testDataRepository.AddTestDonors(metaDonorRepository.AllMetaDonors().ToList().SelectMany(md => md.GetDatabaseDonors()));
        }
    }
}