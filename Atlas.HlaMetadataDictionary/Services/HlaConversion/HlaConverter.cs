using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal interface IHlaConverter
    {
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour);
    }

    internal class HlaConversionBehaviour
    {
        public TargetHlaCategory TargetHlaCategory { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }

    internal class HlaConverter : IHlaConverter
    {
        private readonly IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter;
        private readonly IHlaScoringMetadataService scoringMetadataService;

        public HlaConverter(
            IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter,
            IHlaScoringMetadataService scoringMetadataService)
        {
            this.hlaNameToTwoFieldAlleleConverter = hlaNameToTwoFieldAlleleConverter;
            this.scoringMetadataService = scoringMetadataService;
        }

        public async Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour)
        {
            if (hlaName.IsNullOrEmpty() || conversionBehaviour == null)
            {
                throw new ArgumentNullException();
            }

            switch (conversionBehaviour.TargetHlaCategory)
            {
                case TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix:
                    return await hlaNameToTwoFieldAlleleConverter.ConvertHla(locus, hlaName, ExpressionSuffixBehaviour.Include);
                case TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix:
                    return await hlaNameToTwoFieldAlleleConverter.ConvertHla(locus, hlaName, ExpressionSuffixBehaviour.Exclude);
                case TargetHlaCategory.GGroup:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate GGroup lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion)).MatchingGGroups.ToList();
                case TargetHlaCategory.PGroup:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate PGroup lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion)).MatchingPGroups.ToList();
                case TargetHlaCategory.Serology:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate Serology lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion))
                        .MatchingSerologies.Select(serology => serology.Name).ToList();

                default:
                    throw new ArgumentOutOfRangeException(nameof(conversionBehaviour), conversionBehaviour, null);
            }
        }

        private async Task<IHlaScoringInfo> GetHlaScoringInfo(Locus locus, string hlaName, string hlaNomenclatureVersion)
        {
            return (await scoringMetadataService.GetHlaMetadata(locus, hlaName, hlaNomenclatureVersion)).HlaScoringInfo;
        }
    }
}