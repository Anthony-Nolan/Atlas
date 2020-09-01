using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Models;
using LochNessBuilder;
using Newtonsoft.Json;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.Models.FrequencySetFile>;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile
{
    [Builder]
    internal static class FrequencySetFileBuilder
    {
        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;

        internal static Builder FileWithoutContents()
        {
            var uniqueFileName = $"file-{DateTime.Now:HHmmssffff}.csv";

            return Builder.New
                .With(x => x.FileName, uniqueFileName)
                .With(t => t.UploadedDateTime, DateTime.Now);
        }

        internal static Builder New(string[] registries = null, string[] ethnicity = null, int haplotypeCount = 1, decimal frequencyValue = 0.00001m)
        {
            var frequencySetFile = FrequencySetFileContentsBuilder.NewWithFrequencyCount(ethnicity, registries, haplotypeCount, frequencyValue).Build();

            return FileWithoutContents().With(x => x.Contents, GetStream(frequencySetFile));
        }

        internal static Builder New(
            IEnumerable<HaplotypeFrequency> haplotypeFrequencies,
            string[] registries = null,
            string[] ethnicity = null,
            string nomenclatureVersion = HlaNomenclatureVersion)
        {
            var frequencySetFile = FrequencySetFileContentsBuilder
                .NewWithFrequencies(haplotypeFrequencies, ethnicity, registries)
                .With(f => f.nomenclatureVersion, nomenclatureVersion).Build();

            return FileWithoutContents().With(x => x.Contents, GetStream(frequencySetFile));
        }

        internal static Builder WithHaplotypeFrequencyFileStream(this Builder builder, Stream stream)
        {
            return builder.With(f => f.Contents, stream);
        }

        internal static Builder WithInvalidFormat()
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
}