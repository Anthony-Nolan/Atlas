using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IStaticPatientDataProvider
    {
        void SetTestCase(StaticDataTestCase testCase);
    }

    /// <summary>
    /// Used to select patient data from a static set of patient and donor data.
    /// 
    /// In most cases the PatientDataFactory should be used to create a dataset dynamically,
    /// but in some cases we need to be able to manually specify specific patient hla, along with a set of (database level) donors
    /// </summary>
    public class StaticPatientDataProvider : IStaticPatientDataProvider, IPatientDataProvider
    {
        private readonly IStaticTestHlaRepository testHlaRepository;
        private StaticDataTestCase TestCase { get; set; }

        public StaticPatientDataProvider(IStaticTestHlaRepository testHlaRepository)
        {
            this.testHlaRepository = testHlaRepository;
        }

        public void SetTestCase(StaticDataTestCase testCase)
        {
            TestCase = testCase;
        }

        public PhenotypeInfo<string> GetPatientHla()
        {
            return testHlaRepository.GetPatientHlaData(TestCase);
        }
    }
}