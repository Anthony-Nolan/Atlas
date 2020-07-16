using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile.TestFrequencySetFile>;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile
{
    [Builder]
    internal static class FrequencySetFileBuilder
    {
        private const string CsvHeader = "a;b;c;drb1;dqb1;freq";

        internal static Builder FileWithoutContents(string registryCode, string ethnicityCode)
        {
            var uniqueFileName = $"file-{DateTime.Now:HHmmssffff}.csv";

            return Builder.New
                .With(x => x.FileName, uniqueFileName)
                .With(x => x.FullPath, BuildFilePath(uniqueFileName, registryCode, ethnicityCode));
        }

        internal static Builder New(string registryCode, string ethnicityCode, int haplotypeCount = 1, decimal frequencyValue = 0.00001m)
        {
            return FileWithoutContents(registryCode, ethnicityCode)
                .With(x => x.Contents, GetStream(BuildCsvFile(haplotypeCount, frequencyValue)));
        }

        internal static Builder New(string registryCode, string ethnicityCode, IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            return FileWithoutContents(registryCode, ethnicityCode)
                .With(x => x.Contents, GetStream(BuildCsvFile(haplotypeFrequencies)));
        }

        internal static Builder New(IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            return FileWithoutContents(null, null)
                .With(x => x.Contents, GetStream(BuildCsvFile(haplotypeFrequencies)));
        }

        internal static Builder WithHaplotypeFrequencyFileStream(this Builder builder, Stream stream)
        {
            return builder.With(f => f.Contents, stream);
        }

        /// <param name="registryCode">Path will only contain file name if this is null.</param>
        /// <param name="ethnicityCode">Path will only contain registry code and file name if this is null.</param>
        private static string BuildFilePath(string fileName, string registryCode, string ethnicityCode)
        {
            if (registryCode.IsNullOrEmpty())
            {
                return fileName;
            }

            var path = new StringBuilder(registryCode + "/");

            if (!ethnicityCode.IsNullOrEmpty())
            {
                path.Append(ethnicityCode + "/");
            }

            path.Append(fileName);

            return path.ToString();
        }

        private static string BuildCsvFile(int frequencyCount, decimal frequencyValue)
        {
            var file = new StringBuilder(CsvHeader + Environment.NewLine);

            for (var i = 0; i < frequencyCount; i++)
            {
                var csvFileBodySingleFrequency = $"A-HLA-{i};B-HLA-{i};C-HLA-{i};DRB1-HLA-{i};DQB1-HLA-{i};{frequencyValue}";
                file.AppendLine(csvFileBodySingleFrequency);
            }

            return file.ToString();
        }

        private static string BuildCsvFile(IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            var csvFileBodyFrequencies = haplotypeFrequencies
                .Select(h => $"{h.A};{h.B};{h.C};{h.DRB1};{h.DQB1};{h.Frequency}").ToList();

            var file = new StringBuilder(CsvHeader + Environment.NewLine);

            foreach (var csvFileBodyFrequency in csvFileBodyFrequencies)
            {
                file.AppendLine(csvFileBodyFrequency);
            }

            return file.ToString();
        }

        private static Stream GetStream(string fileContents)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(fileContents));
        }
    }
}