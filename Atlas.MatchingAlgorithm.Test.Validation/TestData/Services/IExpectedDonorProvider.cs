using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    public interface IExpectedDonorProvider
    {
        IEnumerable<int> GetExpectedMatchingDonorIds();
    }
}