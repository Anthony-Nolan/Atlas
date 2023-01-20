using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class LocusBuilder
    {
        private const string DefaultDnaTyping = "01:01";
        private const string DefaultSerologyTyping = "1";

        internal static Builder<ImportedLocus> Default => Builder<ImportedLocus>.New
            .WithDna(DefaultDnaTyping, DefaultDnaTyping)
            .WithSerology(DefaultSerologyTyping, DefaultSerologyTyping);

        internal static Builder<ImportedLocus> WithDna(this Builder<ImportedLocus> builder, string field1, string field2)
        {
            return builder.WithDna(BuildTwoFieldTyping(field1, field2));
        }

        internal static Builder<ImportedLocus> WithDna(this Builder<ImportedLocus> builder, TwoFieldStringData data)
        {
            return builder.With(x => x.Dna, data);
        }

        internal static Builder<ImportedLocus> WithSerology(this Builder<ImportedLocus> builder, string field1, string field2)
        {
            return builder.WithSerology(BuildTwoFieldTyping(field1, field2));
        }

        internal static Builder<ImportedLocus> WithSerology(this Builder<ImportedLocus> builder, TwoFieldStringData data)
        {
            return builder.With(x => x.Serology, data);
        }

        private static TwoFieldStringData BuildTwoFieldTyping(string field1, string field2)
        {
            return new TwoFieldStringData
            {
                Field1 = field1,
                Field2 = field2
            };
        }
    }
}
