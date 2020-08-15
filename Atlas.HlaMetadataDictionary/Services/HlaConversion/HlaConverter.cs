using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal interface IHlaConverter
    {
        Task<bool> ValidateHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour);
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour);

        /// <summary>
        /// Relies on the fact that G groups will have 1 or 0 corresponding P Groups.
        /// </summary>
        /// <returns>
        /// A dictionary of G Group to P Group.
        /// If a GGroup has no corresponding P Group, it will not be present.
        /// </returns>
        Task<string> ConvertGGroupToPGroup(Locus locus, string gGroup, string hlaNomenclatureVersion);
    }

    internal class HlaConversionBehaviour
    {
        public TargetHlaCategory TargetHlaCategory { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }

    internal class HlaConverter : IHlaConverter
    {
        private readonly IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter;
        private readonly IHlaNameToPGroupConverter hlaNameToPGroupConverter;
        private readonly IHlaScoringMetadataService scoringMetadataService;

        public HlaConverter(
            IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter,
            IHlaNameToPGroupConverter hlaNameToPGroupConverter,
            IHlaScoringMetadataService scoringMetadataService)
        {
            this.hlaNameToTwoFieldAlleleConverter = hlaNameToTwoFieldAlleleConverter;
            this.hlaNameToPGroupConverter = hlaNameToPGroupConverter;
            this.scoringMetadataService = scoringMetadataService;
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
                case TargetHlaCategory.PGroup:
                    return await hlaNameToPGroupConverter.ConvertHla(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion);
                case TargetHlaCategory.Serology:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate Serology lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion))
                        .MatchingSerologies.Select(serology => serology.Name).ToList();

                default:
                    throw new ArgumentOutOfRangeException(nameof(conversionBehaviour), conversionBehaviour, null);
            }
        }

        public async Task<bool> ValidateHla(Locus locus, string hlaName, HlaConversionBehaviour validationBehaviour)
        {
            if (hlaName == null || validationBehaviour == null)
            {
                throw new ArgumentNullException();
            }

            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (validationBehaviour.TargetHlaCategory)
            {
                case TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix:
                    throw new NotImplementedException();
                case TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix:
                    throw new NotImplementedException();
                case TargetHlaCategory.GGroup:
                    return (await scoringMetadataService.GetAllGGroups(validationBehaviour.HlaNomenclatureVersion))[locus].Contains(hlaName);
                case TargetHlaCategory.PGroup:
                    throw new NotImplementedException();
                case TargetHlaCategory.Serology:
                    throw new NotImplementedException();

                default:
                    throw new ArgumentOutOfRangeException(nameof(validationBehaviour), validationBehaviour, null);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Converts a single G group to a single P Group (or null), via a cached collection of said lookups.
        /// This is faster than fully expanding G Groups on each call to this method.
        /// </remarks>
        public async Task<string> ConvertGGroupToPGroup(Locus locus, string gGroup, string hlaNomenclatureVersion)
        {
            return await hlaNameToPGroupConverter.ConvertGGroup(locus, gGroup, hlaNomenclatureVersion);
        }

        private async Task<IHlaScoringInfo> GetHlaScoringInfo(Locus locus, string hlaName, string hlaNomenclatureVersion)
        {
            return (await scoringMetadataService.GetHlaMetadata(locus, hlaName, hlaNomenclatureVersion)).HlaScoringInfo;
        }
    }
}