using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.HaplotypeFrequencies;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.GenotypeLikelihood
{
    internal static class FrequenciesFileBuilder
    {
        public static FrequencyFile Build(IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            var uniqueFileName = $"file-{DateTime.Now:HHmmssffff}.csv";

            return new FrequencyFile(uniqueFileName)
            {
                FullPath = uniqueFileName,
                Contents = BuildCsvFile(haplotypeFrequencies)
            };
        }

        private static string BuildCsvFile(IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            const string csvHeader = "a;b;c;drb1;dqb1;freq";
            var csvFileBodyFrequencies = haplotypeFrequencies
                .Select(h => $"{h.A};{h.B};{h.C};{h.DRB1};{h.DQB1};{h.Frequency}").ToList();

            var file = new StringBuilder(csvHeader + Environment.NewLine);

            foreach (var csvFileBodyFrequency in csvFileBodyFrequencies)
            {
                file.AppendLine(csvFileBodyFrequency);
            }

            return file.ToString();
        }
    }
}
