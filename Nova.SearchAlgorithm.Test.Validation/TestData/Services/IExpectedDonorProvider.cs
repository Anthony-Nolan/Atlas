using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services
{
    public interface IExpectedDonorProvider
    {
        IEnumerable<int> GetExpectedMatchingDonorIds();
    }
}