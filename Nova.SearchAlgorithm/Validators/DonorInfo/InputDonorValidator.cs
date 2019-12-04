using FluentValidation;

namespace Nova.SearchAlgorithm.Validators.DonorInfo
{
    public class InputDonorValidator : AbstractValidator<Data.Models.InputDonor>
    {
        public InputDonorValidator()
        {
            RuleFor(x => x.HlaNames).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}