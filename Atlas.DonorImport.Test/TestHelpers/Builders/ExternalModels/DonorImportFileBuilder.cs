using System;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Models;
using AutoFixture.Dsl;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;

internal static class DonorImportFileBuilder
{
    public static IPostprocessComposer<DonorImportFile> NewWithoutContents => FixtureBuilder.For<DonorImportFile>()
        .WithFileLocation("file-location-")
        .WithMessageId("message-id-")
        .With(t => t.UploadTime, DateTime.Now);

    public static IPostprocessComposer<DonorImportFile> NewWithDefaultContents => NewWithoutContents
        .With(f => f.Contents, DonorImportFileContentsBuilder.New.Build().ToStream());

    public static IPostprocessComposer<DonorImportFile> NewWithMetadata(string fileName, string messageId, DateTime uploadTime)
    {
        return FixtureBuilder.For<DonorImportFile>()
            .With(t => t.FileLocation, fileName)
            .With(t => t.MessageId, messageId)
            .With(t => t.UploadTime, uploadTime);
    }

    public static IPostprocessComposer<DonorImportFile> WithContents(
        this IPostprocessComposer<DonorImportFile> builder,
        IPostprocessComposer<SerialisableDonorImportFileContents> contentsBuilder)
    {
        return builder
            .With(f => f.Contents, contentsBuilder.Build().ToStream());
    }

    public static IPostprocessComposer<DonorImportFile> WithDonorCount(this IPostprocessComposer<DonorImportFile> builder, int numberOfDonors, bool isInitialImport = false)
    {
        return builder
            .With(f => f.Contents, DonorImportFileContentsBuilder.New
                .WithDonorCount(numberOfDonors)
                .WithUpdateMode(isInitialImport ? UpdateMode.Full : UpdateMode.Differential)
                .Build()
                .ToStream());
    }

    public static IPostprocessComposer<DonorImportFile> WithDonors(this IPostprocessComposer<DonorImportFile> builder, params DonorUpdate[] donors)
    {
        return builder
            .With(f => f.Contents, DonorImportFileContentsBuilder.New.WithDonors(donors).Build().ToStream());
    }

    public static IPostprocessComposer<DonorImportFile> WithInitialDonors(this IPostprocessComposer<DonorImportFile> builder, params DonorUpdate[] donors)
        => builder.WithInitialDonorsAndUpdateMode(UpdateMode.Full, donors);

    public static IPostprocessComposer<DonorImportFile> WithInitialDonorsAndUpdateMode(this IPostprocessComposer<DonorImportFile> builder, UpdateMode updateMode, params DonorUpdate[] donors)
    {
        return builder
            .With(f => f.Contents, DonorImportFileContentsBuilder.New
                .WithDonors(donors)
                .WithUpdateMode(updateMode)
                .Build()
                .ToStream());
    }

    private static IPostprocessComposer<DonorImportFile> WithFileLocation(this IPostprocessComposer<DonorImportFile> builder, string recordIdPrefix)
    {
        return builder.With(d => d.FileLocation, IncrementingIdGenerator.NextStringIdFactory(recordIdPrefix));
    }

    private static IPostprocessComposer<DonorImportFile> WithMessageId(this IPostprocessComposer<DonorImportFile> builder, string messageIdPrefix)
    {
        return builder.With(d => d.MessageId, IncrementingIdGenerator.NextStringIdFactory(messageIdPrefix));
    }
}