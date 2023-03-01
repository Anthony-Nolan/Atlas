using System.IO;
using Atlas.DonorImport.ExternalInterface.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class DonorIdCheckFileBuilder
    {
        public static Builder<DonorIdCheckFile> New => Builder<DonorIdCheckFile>.New
            .With(f => f.FileLocation, "file-location");

        public static Builder<DonorIdCheckFile> WithContents(this Builder<DonorIdCheckFile> builder, Stream contents) =>
            builder.With(f => f.Contents, contents);
    }
}
