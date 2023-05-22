using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.ManualTesting.Services.WmdaConsensusResults
{
    public interface IWmdaDiscrepantResultsWriter
    {
        Task WriteToFile(string discrepantResultsFilePath, DiscrepantResultsReport report);
    }

    internal class WmdaDiscrepantResultsWriter : IWmdaDiscrepantResultsWriter
    {
        public async Task WriteToFile(string discrepantResultsFilePath, DiscrepantResultsReport report)
        {
            var contents = BuildFileContents(report.AnalysedDiscrepancies);

            var headerText =
                             "Locus;" +
                             "Patient HLA Typing;Donor HLA Typing;" +
                             "Consensus Count;Atlas Count;" +
                             $"([Patient HLA-Donor HLA] Common {report.CommonHlaCategory} [Common {report.CommonHlaCategory} Count]);" +
                             "PDP Count;PDP Ids";

            await File.WriteAllLinesAsync(discrepantResultsFilePath, new[] { headerText }.Concat(contents));
        }

        private static IEnumerable<string> BuildFileContents(IEnumerable<AnalysedDiscrepancy> analysedDiscrepancies)
        {
            static string HlaMetadataToString(CommonHlaMetadataInfo info)
            {
                if (!info.CommonHlaMetadata.Any())
                {
                    return "_";
                }

                const int maxLength = 24;
                var concatHlaMetadata = ConcatenateStrings(info.CommonHlaMetadata);
                var formattedString = concatHlaMetadata.Length > maxLength ? $"{concatHlaMetadata[..maxLength]}..." : concatHlaMetadata;
                return $"([{info.PatientHla}-{info.DonorHla}] {formattedString} [{info.CommonHlaMetadata.Count}])";
            }

            static string ConcatenateStrings(IEnumerable<string> strings) => string.Join(",", strings);

            return analysedDiscrepancies.Select(d =>
                $"{d.Locus};" +
                $"{d.PatientHla.Position1} + {d.PatientHla.Position2};" +
                $"{d.DonorHla.Position1} + {d.DonorHla.Position2};" +
                $"{d.MismatchedCounts.First().ConsensusMismatchCount};" +
                $"{d.MismatchedCounts.First().AtlasMismatchCount};" +
                $"{HlaMetadataToString(d.CommonHlaMetadata.Position1)} + {HlaMetadataToString(d.CommonHlaMetadata.Position2)};" +
                $"{d.MismatchedCounts.Count};" +
                $"{ConcatenateStrings(d.MismatchedCounts.Select(mc => $"{mc.PatientId}:{mc.DonorId}"))}"
            );
        }
    }
}
