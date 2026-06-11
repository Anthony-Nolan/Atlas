using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Models;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Newtonsoft.Json;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchPrediction.Models.FrequencySetFile>;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;

internal static class FrequencySetFileBuilder
{
    private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;

    internal static Composer FileWithoutContents()
    {
        var uniqueFileName = $"file-{DateTime.Now:HHmmssffff}.json";

        return FixtureBuilder.For<Atlas.MatchPrediction.Models.FrequencySetFile>()
            .With(x => x.FileName, uniqueFileName)
            .With(t => t.UploadedDateTime, DateTime.Now);
    }

    internal static Composer New(
        string[] registries = null,
        string[] ethnicity = null,
        int haplotypeCount = 1,
        decimal frequencyValue = 0.00001m,
        ImportTypingCategory typingCategory = ImportTypingCategory.LargeGGroup)
    {
        var frequencySetFile = FrequencySetFileContentsBuilder
            .NewWithFrequencyCount(ethnicity, registries, haplotypeCount, frequencyValue, typingCategory)
            .Build();

        return FileWithoutContents().With(x => x.Contents, GetStream(frequencySetFile));
    }

    internal static Composer New(
        IEnumerable<HaplotypeFrequency> haplotypeFrequencies,
        string[] registries = null,
        string[] ethnicity = null,
        ImportTypingCategory typingCategory = default,
        string nomenclatureVersion = HlaNomenclatureVersion)
    {
        var frequencySetFile = FrequencySetFileContentsBuilder
            .NewWithFrequencies(haplotypeFrequencies, ethnicity, registries, typingCategory)
            .With(f => f.nomenclatureVersion, nomenclatureVersion).Build();

        return FileWithoutContents().With(x => x.Contents, GetStream(frequencySetFile));
    }

    internal static Composer WithHaplotypeFrequencyFileStream(this Composer builder, Stream stream)
    {
        return builder.With(f => f.Contents, stream);
    }

    internal static Composer WithInvalidFormat()
    {
        var frequencySetFile = FrequencySetFileContentsBuilder.NewWithFrequencyCount().Build();

        return FileWithoutContents().With(x => x.Contents, GetInvalidJsonStream(frequencySetFile));
    }

    private static Stream GetStream(SerialisableFrequencySetFileContents frequencySetFile)
    {
        var fileJson = JsonConvert.SerializeObject(frequencySetFile);
        return new MemoryStream(Encoding.Default.GetBytes(fileJson));
    }

    private static Stream GetInvalidJsonStream(SerialisableFrequencySetFileContents frequencySetFile)
    {
        var fileJson = JsonConvert.SerializeObject(frequencySetFile);
        fileJson = fileJson.Substring(fileJson.Length - 10);
        return new MemoryStream(Encoding.Default.GetBytes(fileJson));
    }
}