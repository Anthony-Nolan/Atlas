using System.Linq;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Models.DonorIdCheck;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.DonorIdCheck
{
    [Builder]
    internal static class DonorIdCheckFileContentsBuilder
    {
        private const string RecordIdPrefix = "record-id-";

        public static Builder<SerializableDonorIdCheckerFileContent> New => Builder<SerializableDonorIdCheckerFileContent>.New
            .With(c => c.donPool, "donPool")
            .With(c => c.donorType, ImportDonorType.Adult.ToString())
            .With(c => c.donors, Enumerable.Empty<string>());

        public static Builder<SerializableDonorIdCheckerFileContent> WithDonorIds(
            this Builder<SerializableDonorIdCheckerFileContent> builder,
            int numberOfIds) =>
            builder.With(c => c.donors, Enumerable.Range(0, numberOfIds).Select(id => $"{RecordIdPrefix}{id}"));

        public static Builder<SerializableDonorIdCheckerFileContent> WithDonPool(
            this Builder<SerializableDonorIdCheckerFileContent> builder,
            string donPool) =>
            builder.With(c => c.donPool, donPool);

        public static Builder<SerializableDonorIdCheckerFileContent> WithDonorType(
            this Builder<SerializableDonorIdCheckerFileContent> builder,
            ImportDonorType donorType) =>
            builder.With(c => c.donorType, donorType.ToString());

        public static Builder<SerializableDonorIdCheckerFileContent> WithStringDonorType(
            this Builder<SerializableDonorIdCheckerFileContent> builder,
            string donorType) =>
            builder.With(c => c.donorType, donorType);
    }


    [Builder]
    internal static class InvalidDonorIdCheckFileContentsBuilder
    {
        public static Builder<SerializableDonorIdCheckerFileContentWithInvalidPropertyOrder> FileWithInvalidPropertyOrder => Builder<SerializableDonorIdCheckerFileContentWithInvalidPropertyOrder>.New
            .With(c => c.donPool, "donPool")
            .With(c => c.donorType, ImportDonorType.Adult.ToString())
            .With(c => c.donors, Enumerable.Empty<string>());

        public static Builder<SerializableDonorIdCheckerFileContentWithUnexpectedProperty> FileWithUnexpectedProperty =>
            Builder<SerializableDonorIdCheckerFileContentWithUnexpectedProperty>.New
                .With(c => c.donPool, "donPool")
                .With(c => c.donorType, ImportDonorType.Adult.ToString())
                .With(c => c.donors, Enumerable.Empty<string>());
    }
}
