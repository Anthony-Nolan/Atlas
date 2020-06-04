using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Test.TestHelpers.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels
{
    [Builder]
    internal static class DonorImportFileBuilder
    {
        private static Builder<DonorImportFile> WithDefaultMetaData => Builder<DonorImportFile>.New
            .With(f => f.FileName, "file-name");

        public static Builder<DonorImportFile> New => WithDefaultMetaData
            .With(f => f.Contents, DonorImportFileContentsBuilder.New.Build().ToStream());

        public static Builder<DonorImportFile> WithContents(Builder<SerialisableDonorImportFileContents> contentsBuilder) => WithDefaultMetaData
            .With(f => f.Contents, contentsBuilder.Build().ToStream());
    }
}