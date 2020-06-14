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
    internal class HlaConversionBehaviour
    {
        public TargetHlaOptions TargetHlaOptions { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }

    internal interface IHlaConverter
    {
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour);
    }

    internal class HlaConverter : IHlaConverter
    {
        private readonly IConvertHlaToTwoFieldAlleleService convertHlaToTwoFieldAlleleService;
        private readonly IHlaScoringMetadataService scoringMetadataService;

        public HlaConverter(
            IConvertHlaToTwoFieldAlleleService convertHlaToTwoFieldAlleleService,
            IHlaScoringMetadataService scoringMetadataService)
        {
            this.convertHlaToTwoFieldAlleleService = convertHlaToTwoFieldAlleleService;
            this.scoringMetadataService = scoringMetadataService;
        }

        public async Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour)
        {
            if (hlaName.IsNullOrEmpty() || conversionBehaviour == null)
            {
                throw new ArgumentNullException();
            }
            
            switch (conversionBehaviour.TargetHlaOptions)
            {
                case TargetHlaOptions.TwoFieldAlleleIncludingExpressionSuffix:
                    return await convertHlaToTwoFieldAlleleService.ConvertHla(locus, hlaName, ExpressionSuffixOptions.Include);
                case TargetHlaOptions.TwoFieldAlleleExcludingExpressionSuffix:
                    return await convertHlaToTwoFieldAlleleService.ConvertHla(locus, hlaName, ExpressionSuffixOptions.Exclude);
                case TargetHlaOptions.GGroup:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate GGroup lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion)).MatchingGGroups.ToList();
                case TargetHlaOptions.PGroup:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate PGroup lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion)).MatchingPGroups.ToList();
                case TargetHlaOptions.Serology:
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