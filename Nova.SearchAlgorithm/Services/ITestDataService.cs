using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services
{
    /// <summary>
    /// A service for inserting test data for the convenience of developers.
    /// TODO:NOVA-1151 remove this service before going into production
    /// </summary>
    public interface ITestDataService
    {
        void ImportSingleTestDonor();
        Task ImportSolarDonors();
        Task ImportAllDonorsFromSolar();
        void ImportDummyData();
    }
}