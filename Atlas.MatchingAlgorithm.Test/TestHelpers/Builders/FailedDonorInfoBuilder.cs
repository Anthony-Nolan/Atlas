using Atlas.MatchingAlgorithm.Models;
using LochNessBuilder;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    public static class FailedDonorInfoBuilder
    {
        public static Builder<FailedDonorInfo> New() =>
            Builder<FailedDonorInfo>.New
                .With(x => x.DonorId, Enumerable.Range(1, int.MaxValue).Select(i => $"donor-{i}"));
    }
}
