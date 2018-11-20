using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services
{
    public interface ITestDataService
    {
        void SetupTestData();
    }
    
    public class TestDataService: ITestDataService
    {
        private readonly IMetaDonorRepository metaDonorRepository;

        public TestDataService(IMetaDonorRepository metaDonorRepository)
        {
            this.metaDonorRepository = metaDonorRepository;
        }
        
        public void SetupTestData()
        {
            TestDataRepository.SetupDatabase();
            TestDataRepository.AddTestDonors(metaDonorRepository.AllMetaDonors().ToList().SelectMany(md => md.GetDatabaseDonors()));
        }
    }
}