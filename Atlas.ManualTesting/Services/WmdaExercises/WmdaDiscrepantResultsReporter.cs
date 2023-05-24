using System.Collections.Generic;
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
        /// Compares WMDA consensus file to Atlas results file to identify discrepant mismatch counts.
        /// </summary>
        Task<DiscrepantResultsReport> ReportDiscrepantResults(ReportDiscrepanciesRequest request);
    }

    /// <summary>
    /// Reports discrepant allele (total) mismatch counts.
    /// </summary>
    public interface IWmdaDiscrepantAlleleResultsReporter : IWmdaDiscrepantResultsReporter
    {
    }

    /// <summary>
    /// Reports discrepant antigen mismatch counts.
    /// </summary>
    public interface IWmdaDiscrepantAntigenResultsReporter : IWmdaDiscrepantResultsReporter
    {
    }

    public record DiscrepantResultsReport(TargetHlaCategory CommonHlaCategory, IEnumerable<AnalysedDiscrepancy> AnalysedDiscrepancies);
    public record CommonHlaMetadataInfo(string PatientHla, string DonorHla, IReadOnlyCollection<string> CommonHlaMetadata);
    public record AnalysedDiscrepancy(
        Locus Locus,
        LocusInfo<string> PatientHla,
        LocusInfo<string> DonorHla,
        LocusInfo<CommonHlaMetadataInfo> CommonHlaMetadata,
        IReadOnlyCollection<MismatchCountDetails> MismatchedCounts);

    internal class WmdaDiscrepantResultsReporter : IWmdaDiscrepantAlleleResultsReporter, IWmdaDiscrepantAntigenResultsReporter
    {
        private readonly IAppCache convertedHlaCache;
        private readonly IWmdaResultsComparer resultsComparer;
        private readonly IConvertHlaRequester hlaConversionRequester;
        private readonly TargetHlaCategory targetHlaCategory;

        public WmdaDiscrepantResultsReporter(
            IWmdaResultsComparer resultsComparer,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            ITransientCacheProvider cacheProvider,
            IConvertHlaRequester hlaConversionRequester,
            TargetHlaCategory targetHlaCategory)
        {
            this.resultsComparer = resultsComparer;
            convertedHlaCache = cacheProvider.Cache;
            this.hlaConversionRequester = hlaConversionRequester;
            this.targetHlaCategory = targetHlaCategory;
        }

        /// <inheritdoc />
        public async Task<DiscrepantResultsReport> ReportDiscrepantResults(ReportDiscrepanciesRequest request)
        {
            var discrepantResults = await resultsComparer.GetDiscrepantResults(request);
            var groupedResults = discrepantResults.GroupBy(x => new { x.Locus, x.PatientHla, x.DonorHla });

            var analysedDiscrepancies = new List<AnalysedDiscrepancy>();
            foreach (var discrepancy in groupedResults)
            {
                var locus = discrepancy.Key.Locus;
                var patientHla = discrepancy.Key.PatientHla;
                var donorHla = discrepancy.Key.DonorHla;
                var commonHlaMetadata = await GetCommonHlaMetadataInfo(locus, patientHla, donorHla);

                analysedDiscrepancies.Add(new AnalysedDiscrepancy(
                    locus, patientHla, donorHla, commonHlaMetadata, discrepancy.Select(x => x.MismatchCountDetails).ToList()));
            }

            return new DiscrepantResultsReport(targetHlaCategory, analysedDiscrepancies);
        }

        private async Task<IEnumerable<string>> GetOrAddConvertedHla(Locus locus, string hlaName)
        {
            var cacheKey = $"l{locus};hla{hlaName}";
            return await convertedHlaCache.GetOrAdd(cacheKey, () => hlaConversionRequester.ConvertHla(new ConvertHlaRequest
            {
                Locus = locus,
                HlaName = hlaName,
                TargetHlaCategory = targetHlaCategory
            }));
        }

        private async Task<LocusInfo<CommonHlaMetadataInfo>> GetCommonHlaMetadataInfo(
            Locus locus,
            LocusInfo<string> patientHla,
            LocusInfo<string> donorHla)
        {
            Task<LocusInfo<IEnumerable<string>>> ConvertToTargetHlaCategory(LocusInfo<string> typing) => typing.MapAsync(hla => GetOrAddConvertedHla(locus, hla));
            var patientHlaMetadata = await ConvertToTargetHlaCategory(patientHla);
            var donorHlaMetadata = await ConvertToTargetHlaCategory(donorHla);

            var (isDirectOrientation, commonHlaMetadata) = CalculateCommonHlaMetadata(patientHlaMetadata, donorHlaMetadata);

            return new LocusInfo<CommonHlaMetadataInfo>(
                    new CommonHlaMetadataInfo(patientHla.Position1, isDirectOrientation ? donorHla.Position1 : donorHla.Position2, commonHlaMetadata.Position1),
                    new CommonHlaMetadataInfo(patientHla.Position2, isDirectOrientation ? donorHla.Position2 : donorHla.Position1, commonHlaMetadata.Position2)
                );
        }

        /// <returns>(Is direct orientation?, common HLA metadata)</returns>
        private static (bool, LocusInfo<IReadOnlyCollection<string>>) CalculateCommonHlaMetadata(
            LocusInfo<IEnumerable<string>> patientHlaMetadata,
            LocusInfo<IEnumerable<string>> donorHlaMetadata)
        {
            var directCommonHlaMetadata = CommonHlaMetadataInDirectOrientation(patientHlaMetadata, donorHlaMetadata);
            var crossCommonHlaMetadata = CommonHlaMetadataInCrossOrientation(patientHlaMetadata, donorHlaMetadata);

            static bool BothPositionsHaveCommonHlaMetadata(LocusInfo<IReadOnlyCollection<string>> commonHlaMetadata) =>
                commonHlaMetadata.Position1.Any() && commonHlaMetadata.Position2.Any();

            if (BothPositionsHaveCommonHlaMetadata(directCommonHlaMetadata))
            {
                return (true, directCommonHlaMetadata);
            }

            if (BothPositionsHaveCommonHlaMetadata(crossCommonHlaMetadata))
            {
                return (false, crossCommonHlaMetadata);
            }

            static int TotalHlaMetadataCount(LocusInfo<IReadOnlyCollection<string>> pGroups) => pGroups.Position1.Count + pGroups.Position2.Count;
            return TotalHlaMetadataCount(directCommonHlaMetadata) >= TotalHlaMetadataCount(crossCommonHlaMetadata)
                ? (true, directCommonHlaMetadata)
                : (false, crossCommonHlaMetadata);
        }

        private static LocusInfo<IReadOnlyCollection<string>> CommonHlaMetadataInDirectOrientation(
            LocusInfo<IEnumerable<string>> patientHlaMetadata,
            LocusInfo<IEnumerable<string>> donorHlaMetadata)
        {
            return new LocusInfo<IReadOnlyCollection<string>>(
                CommonHlaMetadata(patientHlaMetadata.Position1, donorHlaMetadata.Position1),
                CommonHlaMetadata(patientHlaMetadata.Position2, donorHlaMetadata.Position2)
            );
        }

        private static LocusInfo<IReadOnlyCollection<string>> CommonHlaMetadataInCrossOrientation(
            LocusInfo<IEnumerable<string>> patientHlaMetadata,
            LocusInfo<IEnumerable<string>> donorHlaMetadata)
        {
            return new LocusInfo<IReadOnlyCollection<string>>(
                CommonHlaMetadata(patientHlaMetadata.Position1, donorHlaMetadata.Position2),
                CommonHlaMetadata(patientHlaMetadata.Position2, donorHlaMetadata.Position1)
            );
        }

        private static IReadOnlyCollection<string> CommonHlaMetadata(IEnumerable<string> patientHlaMetadata, IEnumerable<string> donorHlaMetadata)
        {
            return patientHlaMetadata.Intersect(donorHlaMetadata).ToList();
        }
    }
}