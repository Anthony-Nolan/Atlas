using System.Linq;
using Atlas.DonorImport.Test.TestHelpers.Models.DonorIdCheck;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.DonorIdCheck
{
    [Builder]
    internal static class DonorIdCheckFileContentsBuilder
    {
        public static Builder<SerializableDonorIdCheckerFileContent> New => Builder<SerializableDonorIdCheckerFileContent>.New
            .With(c => c.recordIds, Enumerable.Empty<string>());

        public static Builder<SerializableDonorIdCheckerFileContent> WithDonorIds(
            this Builder<SerializableDonorIdCheckerFileContent> builder,
            int numberOfIds) =>
            builder.With(c => c.recordIds, Enumerable.Range(0, numberOfIds).Select(id => $"donor-id-{id}"));
    }
}
