using System.Linq;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Models.DonorIdCheck;
using AutoFixture.Dsl;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.DonorIdCheck;

internal static class DonorIdCheckFileContentsBuilder
{
    private const string RecordIdPrefix = "record-id-";

    public static IPostprocessComposer<SerializableDonorIdCheckerFileContent> New => FixtureBuilder.For<SerializableDonorIdCheckerFileContent>()
        .With(c => c.donPool, "donPool")
        .With(c => c.donorType, ImportDonorType.Adult.ToString())
        .With(c => c.donors, Enumerable.Empty<string>());

    public static IPostprocessComposer<SerializableDonorIdCheckerFileContent> WithDonorIds(
        this IPostprocessComposer<SerializableDonorIdCheckerFileContent> builder,
        int numberOfIds) =>
        builder.With(c => c.donors, Enumerable.Range(0, numberOfIds).Select(id => $"{RecordIdPrefix}{id}"));

    public static IPostprocessComposer<SerializableDonorIdCheckerFileContent> WithDonPool(
        this IPostprocessComposer<SerializableDonorIdCheckerFileContent> builder,
        string donPool) =>
        builder.With(c => c.donPool, donPool);

    public static IPostprocessComposer<SerializableDonorIdCheckerFileContent> WithDonorType(
        this IPostprocessComposer<SerializableDonorIdCheckerFileContent> builder,
        ImportDonorType donorType) =>
        builder.With(c => c.donorType, donorType.ToString());

    public static IPostprocessComposer<SerializableDonorIdCheckerFileContent> WithStringDonorType(
        this IPostprocessComposer<SerializableDonorIdCheckerFileContent> builder,
        string donorType) =>
        builder.With(c => c.donorType, donorType);
}


internal static class InvalidDonorIdCheckFileContentsBuilder
{
    public static IPostprocessComposer<SerializableDonorIdCheckerFileContentWithInvalidPropertyOrder> FileWithInvalidPropertyOrder => FixtureBuilder.For<SerializableDonorIdCheckerFileContentWithInvalidPropertyOrder>()
        .With(c => c.donPool, "donPool")
        .With(c => c.donorType, ImportDonorType.Adult.ToString())
        .With(c => c.donors, Enumerable.Empty<string>());

    public static IPostprocessComposer<SerializableDonorIdCheckerFileContentWithUnexpectedProperty> FileWithUnexpectedProperty =>
        FixtureBuilder.For<SerializableDonorIdCheckerFileContentWithUnexpectedProperty>()
            .With(c => c.donPool, "donPool")
            .With(c => c.donorType, ImportDonorType.Adult.ToString())
            .With(c => c.donors, Enumerable.Empty<string>());
}