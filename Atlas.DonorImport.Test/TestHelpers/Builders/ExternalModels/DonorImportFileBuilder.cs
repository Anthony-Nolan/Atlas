using System;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels
{
    [Builder]
    internal static class DonorImportFileBuilder
    {
        public static Builder<DonorImportFile> NewWithoutContents => Builder<DonorImportFile>.New
            .WithFileLocation("file-location-")
            .WithMessageId("message-id-")
            .With(t => t.UploadTime, DateTime.Now);

        public static Builder<DonorImportFile> NewWithDefaultContents => NewWithoutContents
            .With(f => f.Contents, DonorImportFileContentsBuilder.New.Build().ToStream());

        public static Builder<DonorImportFile> NewWithMetadata(string fileName, string messageId, DateTime uploadTime)
        {
            return Builder<DonorImportFile>.New
                .With(t => t.FileLocation, fileName)
                .With(t => t.MessageId, messageId)
                .With(t => t.UploadTime, uploadTime);
        }

        public static Builder<DonorImportFile> WithContents(
            this Builder<DonorImportFile> builder,
            Builder<SerialisableDonorImportFileContents> contentsBuilder)
        {
            return builder
                .With(f => f.Contents, contentsBuilder.Build().ToStream());
        }

        public static Builder<DonorImportFile> WithDonorCount(this Builder<DonorImportFile> builder, int numberOfDonors, bool isInitialImport = false)
        {
            return builder
                .With(f => f.Contents, DonorImportFileContentsBuilder.New
                    .WithDonorCount(numberOfDonors)
                    .WithUpdateMode(isInitialImport ? UpdateMode.Full : UpdateMode.Differential)
                    .Build()
                    .ToStream());
        }

        public static Builder<DonorImportFile> WithDonors(this Builder<DonorImportFile> builder, params DonorUpdate[] donors)
        {
            return builder
                .With(f => f.Contents, DonorImportFileContentsBuilder.New.WithDonors(donors).Build().ToStream());
        }

        public static Builder<DonorImportFile> WithInitialDonors(this Builder<DonorImportFile> builder, params DonorUpdate[] donors)
        {
            return builder
                .With(f => f.Contents, DonorImportFileContentsBuilder.New
                    .WithDonors(donors)
                    .WithUpdateMode(UpdateMode.Full)
                    .Build()
                    .ToStream());
        }
        
        private static Builder<DonorImportFile> WithFileLocation(this Builder<DonorImportFile> builder, string recordIdPrefix)
        {
            return builder.WithFactory(d => d.FileLocation, IncrementingIdGenerator.NextStringIdFactory(recordIdPrefix));
        }

        private static Builder<DonorImportFile> WithMessageId(this Builder<DonorImportFile> builder, string messageIdPrefix)
        {
            return builder.WithFactory(d => d.MessageId, IncrementingIdGenerator.NextStringIdFactory(messageIdPrefix));
        }
    }
}