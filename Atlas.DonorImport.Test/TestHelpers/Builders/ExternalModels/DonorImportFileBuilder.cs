using System;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Test.TestHelpers.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels
{
    [Builder]
    internal static class DonorImportFileBuilder
    {
        public static Builder<DonorImportFile> NewWithoutContents => Builder<DonorImportFile>.New
            .WithFileLocation("file-location-")
            .With(t => t.UploadTime, DateTime.Now);

        public static Builder<DonorImportFile> NewWithDefaultContents => NewWithoutContents
            .With(f => f.Contents, DonorImportFileContentsBuilder.New.Build().ToStream());

        public static Builder<DonorImportFile> WithContents(
            this Builder<DonorImportFile> builder,
            Builder<SerialisableDonorImportFileContents> contentsBuilder)
        {
            return builder
                .With(f => f.Contents, contentsBuilder.Build().ToStream());
        }

        public static Builder<DonorImportFile> WithDonorCount(this Builder<DonorImportFile> builder, int numberOfDonors)
        {
            return builder
                .With(f => f.Contents, DonorImportFileContentsBuilder.New.WithDonorCount(numberOfDonors).Build().ToStream());
        }

        public static Builder<DonorImportFile> WithDonors(this Builder<DonorImportFile> builder, params DonorUpdate[] donors)
        {
            return builder
                .With(f => f.Contents, DonorImportFileContentsBuilder.New.WithDonors(donors).Build().ToStream());
        }
        
        private static Builder<DonorImportFile> WithFileLocation(this Builder<DonorImportFile> builder, string recordIdPrefix)
        {
            return builder.WithFactory(d => d.FileLocation, IncrementingIdGenerator.NextStringIdFactory(recordIdPrefix));
        }
    }
}