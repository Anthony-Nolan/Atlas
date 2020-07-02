using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.Hla
{
    public class PhenotypeHlaNamesValidator : AbstractValidator<PhenotypeInfo<string>>
    {
        public PhenotypeHlaNamesValidator()
        {
            RuleFor(x => x.A).NotNull().SetValidator(new RequiredLocusHlaNamesValidator());
            RuleFor(x => x.B).NotNull().SetValidator(new RequiredLocusHlaNamesValidator());
            RuleFor(x => x.Drb1).NotNull().SetValidator(new RequiredLocusHlaNamesValidator());

            RuleFor(x => x.C).SetValidator(new OptionalLocusHlaNamesValidator());
            RuleFor(x => x.Dqb1).SetValidator(new OptionalLocusHlaNamesValidator());
            RuleFor(x => x.Dpb1).SetValidator(new OptionalLocusHlaNamesValidator());
        }
    }

    public class OptionalLocusHlaNamesValidator : AbstractValidator<LocusInfo<string>>
    {
        public OptionalLocusHlaNamesValidator()
        {
            RuleFor(x => x.Position1).NotEmpty().When(x => !x.Position2.IsNullOrEmpty());
            RuleFor(x => x.Position2).NotEmpty().When(x => !x.Position1.IsNullOrEmpty());
        }
    }

    public class RequiredLocusHlaNamesValidator : AbstractValidator<LocusInfo<string>>
    {
        public RequiredLocusHlaNamesValidator()
        {
            RuleFor(x => x.Position1).NotEmpty();
            RuleFor(x => x.Position2).NotEmpty();
        }
    }
}