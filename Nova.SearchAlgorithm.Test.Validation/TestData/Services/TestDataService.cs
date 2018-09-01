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
        private readonly IStaticTestHlaRepository staticTestHlaRepository;

        public TestDataService(IMetaDonorRepository metaDonorRepository, IStaticTestHlaRepository staticTestHlaRepository)
        {
            this.metaDonorRepository = metaDonorRepository;
            this.staticTestHlaRepository = staticTestHlaRepository;
        }
        
        public void SetupTestData()
        {
            TestDataRepository.SetupDatabase();
            TestDataRepository.AddTestDonors(metaDonorRepository.AllMetaDonors().ToList().SelectMany(md => md.GetDatabaseDonors()));
            TestDataRepository.AddTestDonors(staticTestHlaRepository.GetAllDonors());
        }
    }
}