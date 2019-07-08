using Nova.SearchAlgorithm.Clients;
using Nova.SearchAlgorithm.Clients.Http;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers
{
    public static class MockClientManager
    {
        public static IDonorServiceClient DonorServiceClient { get; set; }
    }
}