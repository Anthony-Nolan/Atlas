using Atlas.MatchingAlgorithm.Clients;
using Atlas.MatchingAlgorithm.Clients.Http;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers
{
    public static class MockClientManager
    {
        public static IDonorServiceClient DonorServiceClient { get; set; }
    }
}