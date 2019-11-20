using FluentValidation;
using Nova.Utils.PhenotypeInfo;

namespace Nova.SearchAlgorithm.Validators.InputDonor
{
    public class RequiredLocusHlaNamesValidator : AbstractValidator<LocusInfo<string>>
    {
        public RequiredLocusHlaNamesValidator()
        {
            RuleFor(x => x.Locus).IsInEnum();
            RuleFor(x => x.Position1).NotEmpty();
            RuleFor(x => x.Position2).NotEmpty();
        }
    }
}