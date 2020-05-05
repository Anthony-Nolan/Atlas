using Atlas.MatchingAlgorithm.Clients.Http.DonorService;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers
{
    public static class MockClientManager
    {
        public static IDonorServiceClient DonorServiceClient { get; set; }
    }
}