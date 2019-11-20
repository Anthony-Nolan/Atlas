using FluentValidation;

namespace Nova.SearchAlgorithm.Validators.InputDonor
{
    public class InputDonorValidator : AbstractValidator<Client.Models.Donors.InputDonor>
    {
        public InputDonorValidator()
        {
            RuleFor(x => x.HlaNames).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}