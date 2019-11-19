using FluentValidation;
using Nova.Utils.PhenotypeInfo;

namespace Nova.SearchAlgorithm.Validators.InputDonor
{
    public class PhenotypeHlaNamesValidator : AbstractValidator<PhenotypeInfo<string>>
    {
        public PhenotypeHlaNamesValidator()
        {
            RuleFor(x => x.A).NotNull().SetValidator(new LocusHlaNamesValidator());
            RuleFor(x => x.B).NotNull().SetValidator(new LocusHlaNamesValidator());
            RuleFor(x => x.Drb1).NotNull().SetValidator(new LocusHlaNamesValidator());
            RuleFor(x => x.C).SetValidator(new LocusHlaNamesValidator());
            RuleFor(x => x.Dqb1).SetValidator(new LocusHlaNamesValidator());
            RuleFor(x => x.Dpb1).SetValidator(new LocusHlaNamesValidator());
        }
    }
}