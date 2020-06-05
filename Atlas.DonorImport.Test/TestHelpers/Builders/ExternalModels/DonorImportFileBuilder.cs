using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Test.TestHelpers.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels
{
    [Builder]
    internal static class DonorImportFileBuilder
    {
        public static Builder<DonorImportFile> WithDefaultMetaData => Builder<DonorImportFile>.New
            .With(f => f.FileName, "file-name");

        public static Builder<DonorImportFile> New => WithDefaultMetaData
            .With(f => f.Contents, DonorImportFileContentsBuilder.New.Build().ToStream());

        public static Builder<DonorImportFile> WithContents(
            this Builder<DonorImportFile> builder,
            Builder<SerialisableDonorImportFileContents> contentsBuilder)
        {
            return builder
                .With(f => f.Contents, contentsBuilder.Build().ToStream());
        }

        public static Builder<DonorImportFile> WithDonors(this Builder<DonorImportFile> builder, int numberOfDonors)
        {
            return builder
                .With(f => f.Contents, DonorImportFileContentsBuilder.New.WithDonors(numberOfDonors).Build().ToStream());
        }

        public static Builder<DonorImportFile> WithDonors(this Builder<DonorImportFile> builder, params DonorUpdate[] donors)
        {
            return builder
                .With(f => f.Contents, DonorImportFileContentsBuilder.New.WithDonors(donors).Build().ToStream());
        }
    }
}