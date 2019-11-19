using Castle.Core.Internal;
using FluentValidation;
using Nova.Utils.PhenotypeInfo;

namespace Nova.SearchAlgorithm.Validators.InputDonor
{
    public class LocusHlaNamesValidator : AbstractValidator<LocusInfo<string>>
    {
        public LocusHlaNamesValidator()
        {
            RuleFor(x => x.Locus).IsInEnum();
            RuleFor(x => x.Position1).NotEmpty().When(x => !x.Position2.IsNullOrEmpty());
            RuleFor(x => x.Position2).NotEmpty().When(x => !x.Position1.IsNullOrEmpty());
        }
    }
}