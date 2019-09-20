using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services
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
            testDataRepository.SetupPersistentDatabase();
            testDataRepository.SetupDatabase();
            testDataRepository.AddTestDonors(metaDonorRepository.AllMetaDonors().ToList().SelectMany(md => md.GetDatabaseDonors()));
        }
    }
}