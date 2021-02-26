using System;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;

namespace Atlas.HlaMetadataDictionary.Services.HlaValidation
{
    internal interface IHlaValidator
    {
        Task<bool> ValidateHla(Locus locus, string hlaName, HlaValidationBehaviour conversionBehaviour);
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

        public async Task<bool> ValidateHla(Locus locus, string hlaName, HlaValidationBehaviour validationBehaviour)
        {
            if (hlaName.IsNullOrEmpty() || validationBehaviour == null)
            {
                throw new ArgumentNullException();
            }
            
            switch (validationBehaviour.TargetHlaCategory)
            {
                case HlaValidationCategory.GGroup:
                    return (await scoringMetadataService.GetAllGGroups(validationBehaviour.HlaNomenclatureVersion))[locus].Contains(hlaName);
                case HlaValidationCategory.SmallGGroup:
                    var allGroups = await smallGGroupMetadataService.GetAllSmallGGroups(validationBehaviour.HlaNomenclatureVersion);
                    return allGroups[locus].Contains(hlaName);
                default:
                    throw new ArgumentOutOfRangeException(nameof(validationBehaviour), validationBehaviour, null);
            }
        }
    }
}