using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using FluentValidation;

namespace Atlas.Common.Validation
{
    public class PhenotypeHlaNamesValidator : AbstractValidator<PhenotypeInfoTransfer<string>>
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

    public class OptionalLocusHlaNamesValidator : AbstractValidator<LocusInfoTransfer<string>>
    {
        public OptionalLocusHlaNamesValidator()
        {
            RuleFor(x => x.Position1).NotEmpty().When(x => !x.Position2.IsNullOrEmpty());
            RuleFor(x => x.Position2).NotEmpty().When(x => !x.Position1.IsNullOrEmpty());
        }
    }

    public class RequiredLocusHlaNamesValidator : AbstractValidator<LocusInfoTransfer<string>>
    {
        public RequiredLocusHlaNamesValidator(string message = null)
        {
            if (message != null)
            {
                RuleFor(x => x.Position1).NotEmpty().WithMessage(message);
                RuleFor(x => x.Position2).NotEmpty().WithMessage(message);
            }
            else
            {
                RuleFor(x => x.Position1).NotEmpty();
                RuleFor(x => x.Position2).NotEmpty();
            }
        }
    }
}