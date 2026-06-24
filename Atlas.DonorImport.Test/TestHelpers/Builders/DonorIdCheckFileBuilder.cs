using System.IO;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.ExternalInterface.Models;
using AutoFixture.Dsl;

namespace Atlas.DonorImport.Test.TestHelpers.Builders;

internal static class DonorIdCheckFileBuilder
{
    public static IPostprocessComposer<DonorIdCheckFile> New => FixtureBuilder.For<DonorIdCheckFile>()
        .With(f => f.FileLocation, "file-location");

    public static IPostprocessComposer<DonorIdCheckFile> WithContents(this IPostprocessComposer<DonorIdCheckFile> builder, Stream contents) =>
        builder.With(f => f.Contents, contents);
}