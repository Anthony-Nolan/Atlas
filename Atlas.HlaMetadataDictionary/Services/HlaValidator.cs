using System;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;

namespace Atlas.HlaMetadataDictionary.Services
{
    internal interface IHlaValidator
    {
        Task<bool> ValidateHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour);
    }

    internal class HlaValidator : IHlaValidator
    {
        private readonly IHlaScoringMetadataService scoringMetadataService;
        private readonly ISmallGGroupMetadataService smallGGroupMetadataService;

        public HlaValidator(IHlaScoringMetadataService scoringMetadataService, ISmallGGroupMetadataService smallGGroupMetadataService)
        {
            this.scoringMetadataService = scoringMetadataService;
            this.smallGGroupMetadataService = smallGGroupMetadataService;
        }

        public async Task<bool> ValidateHla(Locus locus, string hlaName, HlaConversionBehaviour validationBehaviour)
        {
            if (hlaName == null || validationBehaviour == null)
            {
                throw new ArgumentNullException();
            }

            // TODO: ATLAS-881 - Clean up this method; it is currently modeled on the HLA converter, but it
            // has little in common with it. E.g., should NOT be using the TargetHlaCategory enum; "TwoFieldAllele"
            // isn't a validator option. Would make more sense to create a new enum just for supported HLA categories.

            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (validationBehaviour.TargetHlaCategory)
            {
                case TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix:
                    throw new NotImplementedException();
                case TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix:
                    throw new NotImplementedException();
                case TargetHlaCategory.GGroup:
                    return (await scoringMetadataService.GetAllGGroups(validationBehaviour.HlaNomenclatureVersion))[locus].Contains(hlaName);
                case TargetHlaCategory.SmallGGroup:
                    var allGroups = await smallGGroupMetadataService.GetAllSmallGGroups(validationBehaviour.HlaNomenclatureVersion);
                    return allGroups[locus].Contains(hlaName);
                case TargetHlaCategory.PGroup:
                    throw new NotImplementedException();
                case TargetHlaCategory.Serology:
                    throw new NotImplementedException();

                default:
                    throw new ArgumentOutOfRangeException(nameof(validationBehaviour), validationBehaviour, null);
            }
        }
    }
}