using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal interface IHlaConverter
    {
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour);
    }

    internal class HlaConverter : IHlaConverter
    {
        private readonly IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter;
        private readonly IHlaScoringMetadataService scoringMetadataService;
        private readonly ISmallGGroupMetadataService smallGGroupMetadataService;

        public HlaConverter(
            IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter,
            IHlaScoringMetadataService scoringMetadataService,
            ISmallGGroupMetadataService smallGGroupMetadataService)
        {
            this.hlaNameToTwoFieldAlleleConverter = hlaNameToTwoFieldAlleleConverter;
            this.scoringMetadataService = scoringMetadataService;
            this.smallGGroupMetadataService = smallGGroupMetadataService;
        }

        public async Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour)
        {
            if (hlaName.IsNullOrEmpty() || conversionBehaviour == null)
            {
                throw new ArgumentNullException();
            }

            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (conversionBehaviour.TargetHlaCategory)
            {
                case TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix:
                    return await hlaNameToTwoFieldAlleleConverter.ConvertHla(
                        locus, hlaName, ExpressionSuffixBehaviour.Include, conversionBehaviour.HlaNomenclatureVersion);
                case TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix:
                    return await hlaNameToTwoFieldAlleleConverter.ConvertHla(
                        locus, hlaName, ExpressionSuffixBehaviour.Exclude, conversionBehaviour.HlaNomenclatureVersion);
                case TargetHlaCategory.GGroup:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate GGroup lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion)).MatchingGGroups.ToList();
                case TargetHlaCategory.SmallGGroup:
                    return (await smallGGroupMetadataService.GetSmallGGroups(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion)).ToList();
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