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

        public HlaValidator(IHlaScoringMetadataService scoringMetadataService)
        {
            this.scoringMetadataService = scoringMetadataService;
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
                    // TODO: ATLAS-881 Validate small g groups
                    throw new NotImplementedException();
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