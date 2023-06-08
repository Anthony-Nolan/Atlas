using Atlas.ManualTesting.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Atlas.ManualTesting.Services.HaplotypeFrequencySet
{
    public interface ITransformedSetWriter
    {
        Task WriteTransformedSet(TransformHaplotypeFrequencySetRequest request, TransformedHaplotypeFrequencySet set);
    }

    internal class TransformedSetWriter : ITransformedSetWriter
    {
        private record OutputFileInfo(string Directory, string OriginalFileName, string FilePrefix);

        public async Task WriteTransformedSet(TransformHaplotypeFrequencySetRequest request, TransformedHaplotypeFrequencySet transformedSet)
        {
            var fileInfo = GetFileInfo(request);

            await WriteFrequencySet(fileInfo, transformedSet.Set);

            await WriteLog(
                fileInfo, 
                transformedSet.OriginalRecordCount,
                transformedSet.Set.Frequencies.Count(),
                transformedSet.OriginalRecordsContainingTarget);
        }

        private static OutputFileInfo GetFileInfo(TransformHaplotypeFrequencySetRequest request)
        {
            string StripChars(string input) => Regex.Replace(input, @"[\*\:]", "");

            var directory = Path.GetDirectoryName(request.HaplotypeFrequencySetFilePath);
            var fileName = Path.GetFileName(request.HaplotypeFrequencySetFilePath);
            var fileNamePrefix = $"{request.FindReplaceHlaNames.Locus}_{StripChars(request.FindReplaceHlaNames.TargetHlaName)}-to-{StripChars(request.FindReplaceHlaNames.ReplacementHlaName)}_";
            return new OutputFileInfo(directory, fileName, fileNamePrefix);
        }

        private static async Task WriteFrequencySet(OutputFileInfo fileInfo, FrequencySetFileSchema set)
        {
            var resultsPath = fileInfo.Directory + $"/{fileInfo.FilePrefix}{fileInfo.OriginalFileName}";

            var contents = JsonConvert.SerializeObject(set, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                Formatting = Formatting.Indented
            });

            await File.WriteAllTextAsync(resultsPath, contents);
        }

        private static async Task WriteLog(
            OutputFileInfo fileInfo, 
            int originalRecordCount,
            int newRecordCount,
            IEnumerable<FrequencyRecord> originalRecordsContainingTarget)
        {
            var newFileName = $"/{fileInfo.FilePrefix}log_{Path.GetFileNameWithoutExtension(fileInfo.OriginalFileName)}.txt";
            var resultsPath = fileInfo.Directory + newFileName;

            var contents = new List<string>
            {
                $"# Record counts: Original file = {originalRecordCount}, New file = {newRecordCount}",
                "# Original records that contained the target HLA name (A~B~C~DQB1~DRB1;Frequency):"
            };
            contents.AddRange(originalRecordsContainingTarget.Select(o => $"{o.A}~{o.B}~{o.C}~{o.Dqb1}~{o.Drb1};{o.Frequency}"));

            await File.WriteAllLinesAsync(resultsPath, contents);
        }
    }
}