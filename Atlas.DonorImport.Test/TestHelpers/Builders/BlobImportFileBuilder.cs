using System.IO;
using Atlas.DonorImport.ExternalInterface.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class BlobImportFileBuilder
    {
        public static Builder<BlobImportFile> New => Builder<BlobImportFile>.New
            .With(f => f.FileLocation, "file-location");

        public static Builder<BlobImportFile> WithContents(this Builder<BlobImportFile> builder, Stream contents) =>
            builder.With(f => f.Contents, contents);
    }
}
