using Atlas.Common.Validation;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using FluentValidation;

namespace Atlas.MatchPrediction.Validators
{
    internal class MatchProbabilityInputValidator : AbstractValidator<SingleDonorMatchProbabilityInput>
    {
        public MatchProbabilityInputValidator()
        {
            RuleFor(i => i.Donor).NotNull().SetValidator(new MatchProbabilityDonorInputValidator());
            RuleFor(i => i.PatientHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
            RuleFor(i => i.HlaNomenclatureVersion).NotEmpty();
        }
    }

    internal class MatchProbabilityDonorInputValidator : AbstractValidator<DonorInput>
    {
        public MatchProbabilityDonorInputValidator()
        {
            RuleFor(i => i.DonorHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}