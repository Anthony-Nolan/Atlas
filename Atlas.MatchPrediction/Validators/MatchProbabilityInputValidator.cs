using Atlas.Common.Validation;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using FluentValidation;

namespace Atlas.MatchPrediction.Validators
{
    internal class MatchProbabilityInputValidator : AbstractValidator<MatchProbabilityInput>
    {
        public MatchProbabilityInputValidator()
        {
            RuleFor(i => i.DonorHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
            RuleFor(i => i.PatientHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
            RuleFor(i => i.HlaNomenclatureVersion).NotEmpty();
        }
    }
}