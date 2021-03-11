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
            testDataRepository.SetupPersistentMatchingDatabase();
            testDataRepository.SetupTransientMatchingDatabase();
            testDataRepository.SetUpDonorDatabase();
            testDataRepository.AddTestDonors(metaDonorRepository.AllMetaDonors().ToList().SelectMany(md => md.GetDatabaseDonors()));
        }
    }
}