using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.ManualTesting.Models;
using LazyCache;

namespace Atlas.ManualTesting.Services.WmdaConsensusResults
{
    public interface IWmdaDiscrepantResultsReporter
    {
        /// <summary>
        /// Compares WMDA consensus file to Atlas results file to identify discrepant mismatch counts,
        /// and writes them to a text file within the same directory as the Atlas results file.
        /// </summary>
        Task ReportDiscrepantResults(ReportDiscrepanciesRequest request);
    }

    internal class WmdaDiscrepantResultsReporter : IWmdaDiscrepantResultsReporter
    {
        private readonly IAppCache pGroupsCache;
        private readonly IWmdaResultsComparer resultsComparer;
        private readonly IConvertHlaRequester hlaConverter;

        private record CommonPGroupInfo(string PatientHla, string DonorHla, IReadOnlyCollection<string> CommonPGroups);
        private record AnalysedDiscrepancy(
            Locus Locus,
            LocusInfo<string> PatientHla,
            LocusInfo<string> DonorHla,
            LocusInfo<CommonPGroupInfo> CommonPGroups,
            IReadOnlyCollection<MismatchCountDetails> MismatchedCounts);

        public WmdaDiscrepantResultsReporter(
            IWmdaResultsComparer resultsComparer,
            ITransientCacheProvider cacheProvider,
            IConvertHlaRequester hlaConverter)
        {
            this.resultsComparer = resultsComparer;
            pGroupsCache = cacheProvider.Cache;
            this.hlaConverter = hlaConverter;
        }

        /// <inheritdoc />
        public async Task ReportDiscrepantResults(ReportDiscrepanciesRequest request)
        {
            var discrepantResults = await resultsComparer.GetDiscrepantResults(request);
            var groupedResults = discrepantResults.GroupBy(x => new { x.Locus, x.PatientHla, x.DonorHla });

            var analysedDiscrepancies = new List<AnalysedDiscrepancy>();
            foreach (var discrepancy in groupedResults)
            {
                var locus = discrepancy.Key.Locus;
                var patientHla = discrepancy.Key.PatientHla;
                var donorHla = discrepancy.Key.DonorHla;
                var commonPGroups = await GetCommonPGroupInfo(locus, patientHla, donorHla);

                analysedDiscrepancies.Add(new AnalysedDiscrepancy(
                    locus, patientHla, donorHla, commonPGroups, discrepancy.Select(x => x.MismatchCountDetails).ToList()));
            }

            await WriteAnalysedDiscrepanciesToFile(request.ResultsFilePath, analysedDiscrepancies);
        }

        public async Task<IEnumerable<string>> GetOrAddPGroups(Locus locus, string hlaName)
        {
            var cacheKey = $"l{locus};hla{hlaName}";
            return await pGroupsCache.GetOrAdd(cacheKey, () => hlaConverter.ConvertHla(new ConvertHlaRequest
            {
                Locus = locus,
                HlaName = hlaName,
                TargetHlaCategory = TargetHlaCategory.PGroup
            }));
        }

        private async Task<LocusInfo<CommonPGroupInfo>> GetCommonPGroupInfo(
            Locus locus,
            LocusInfo<string> patientHla,
            LocusInfo<string> donorHla)
        {
            Task<LocusInfo<IEnumerable<string>>> ConvertToPGroups(LocusInfo<string> typing) => typing.MapAsync(hla => GetOrAddPGroups(locus, hla));
            var patientPGroups = await ConvertToPGroups(patientHla);
            var donorPGroups = await ConvertToPGroups(donorHla);

            var (isDirectOrientation, commonPGroups) = CalculateCommonPGroups(patientPGroups, donorPGroups);

            return new LocusInfo<CommonPGroupInfo>(
                    new CommonPGroupInfo(patientHla.Position1, isDirectOrientation ? donorHla.Position1 : donorHla.Position2, commonPGroups.Position1),
                    new CommonPGroupInfo(patientHla.Position2, isDirectOrientation ? donorHla.Position2 : donorHla.Position1, commonPGroups.Position2)
                );
        }

        /// <returns>(Is direct orientation?, common P groups)</returns>
        private static (bool, LocusInfo<IReadOnlyCollection<string>>) CalculateCommonPGroups(
            LocusInfo<IEnumerable<string>> patientPGroups,
            LocusInfo<IEnumerable<string>> donorPGroups)
        {
            var directCommonPGroups = CommonPGroupsInDirectOrientation(patientPGroups, donorPGroups);
            var crossCommonPGroups = CommonPGroupsInCrossOrientation(patientPGroups, donorPGroups);

            static bool BothPositionsHaveCommonPGroups(LocusInfo<IReadOnlyCollection<string>> commonPGroups) =>
                commonPGroups.Position1.Any() && commonPGroups.Position2.Any();

            if (BothPositionsHaveCommonPGroups(directCommonPGroups))
            {
                return (true, directCommonPGroups);
            }

            if (BothPositionsHaveCommonPGroups(crossCommonPGroups))
            {
                return (false, crossCommonPGroups);
            }

            static int TotalPGroupCount(LocusInfo<IReadOnlyCollection<string>> pGroups) => pGroups.Position1.Count + pGroups.Position2.Count;
            return TotalPGroupCount(directCommonPGroups) >= TotalPGroupCount(crossCommonPGroups)
                ? (true, directCommonPGroups)
                : (false, crossCommonPGroups);
        }

        private static LocusInfo<IReadOnlyCollection<string>> CommonPGroupsInDirectOrientation(
            LocusInfo<IEnumerable<string>> patientPGroups,
            LocusInfo<IEnumerable<string>> donorPGroups)
        {
            return new LocusInfo<IReadOnlyCollection<string>>(
                CommonPGroups(patientPGroups.Position1, donorPGroups.Position1),
                CommonPGroups(patientPGroups.Position2, donorPGroups.Position2)
            );
        }

        private static LocusInfo<IReadOnlyCollection<string>> CommonPGroupsInCrossOrientation(
            LocusInfo<IEnumerable<string>> patientPGroups,
            LocusInfo<IEnumerable<string>> donorPGroups)
        {
            return new LocusInfo<IReadOnlyCollection<string>>(
                CommonPGroups(patientPGroups.Position1, donorPGroups.Position2),
                CommonPGroups(patientPGroups.Position2, donorPGroups.Position1)
            );
        }

        private static IReadOnlyCollection<string> CommonPGroups(IEnumerable<string> patientPGroups, IEnumerable<string> donorPGroups)
        {
            return patientPGroups.Intersect(donorPGroups).ToList();
        }

        private static async Task WriteAnalysedDiscrepanciesToFile(string resultsFilePath, IEnumerable<AnalysedDiscrepancy> analysedDiscrepancies)
        {
            var outputPath =
                $"{Path.GetDirectoryName(resultsFilePath)}/" +
                $"{Path.GetFileNameWithoutExtension(resultsFilePath)}-discrepancies.txt";

            var contents = BuildFileContents(analysedDiscrepancies);
            const string headerText = "Locus;Patient HLA Typing;Donor HLA Typing;Consensus Count;Atlas Count;([Patient HLA-Donor HLA] Common PGroups [Common PGroup Count]);PDP Count;PDP Ids";
            await File.WriteAllLinesAsync(outputPath, new[] { headerText }.Concat(contents));
        }

        private static IEnumerable<string> BuildFileContents(IEnumerable<AnalysedDiscrepancy> analysedDiscrepancies)
        {
            static string PGroupsToString(CommonPGroupInfo info)
            {
                if (!info.CommonPGroups.Any())
                {
                    return "_";
                }

                const int maxLength = 24;
                var concatPGroups = ConcatenateStrings(info.CommonPGroups);
                var pGroupStr = concatPGroups.Length > maxLength ? $"{concatPGroups[..maxLength]}..." : concatPGroups;
                return $"([{info.PatientHla}-{info.DonorHla}] {pGroupStr} [{info.CommonPGroups.Count}])";
            }

            static string ConcatenateStrings(IEnumerable<string> strings) => string.Join(",", strings);

            return analysedDiscrepancies.Select(d =>
                $"{d.Locus};" +
                $"{d.PatientHla.Position1} + {d.PatientHla.Position2};" +
                $"{d.DonorHla.Position1} + {d.DonorHla.Position2};" +
                $"{d.MismatchedCounts.First().ConsensusMismatchCount};" +
                $"{d.MismatchedCounts.First().AtlasMismatchCount};" +
                $"{PGroupsToString(d.CommonPGroups.Position1)} + {PGroupsToString(d.CommonPGroups.Position2)};" +
                $"{d.MismatchedCounts.Count};" +
                $"{ConcatenateStrings(d.MismatchedCounts.Select(mc => $"{mc.PatientId}:{mc.DonorId}"))}"
            );
        }
    }
}