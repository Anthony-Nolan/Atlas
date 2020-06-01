using Atlas.Common.Utils.Extensions;
using System;
using System.Text;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.HaplotypeFrequencies
{
    internal static class FrequencyFileBuilder
    {
        public static FrequencyFile Build(string registryCode, string ethnicityCode, int haplotypeCount = 1, decimal frequencyValue = 0.00001m)
        {
            var uniqueFileName = $"file-{DateTime.Now:HHmmssffff}.csv";

            return new FrequencyFile(uniqueFileName)
            {
                FullPath = BuildFilePath(uniqueFileName, registryCode, ethnicityCode),
                Contents = BuildCsvFile(haplotypeCount, frequencyValue)
            };
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
            const string csvHeader = "a;b;c;drb1;dqb1;freq";
            var csvFileBodySingleFrequency = $"A-HLA;B-HLA;C-HLA;DRB1-HLA;DQB1-HLA;{frequencyValue}";

            var file = new StringBuilder(csvHeader + Environment.NewLine);

            for (var i = 0; i < frequencyCount; i++)
            {
                file.AppendLine(csvFileBodySingleFrequency);
            }

            return file.ToString();
        }
    }
}
