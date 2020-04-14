using LochNessBuilder;
using Atlas.MatchingAlgorithm.Models;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    public static class FailedDonorInfoBuilder
    {
        public static Builder<FailedDonorInfo> New(IEnumerable<string> registries = null) =>
            Builder<FailedDonorInfo>.New
                .With(x => x.DonorId, Enumerable.Range(1, int.MaxValue).Select(i => $"donor-{i}"))
                .With(x => x.RegistryCode, registries ?? new[] { "registry-code" });
    }
}
