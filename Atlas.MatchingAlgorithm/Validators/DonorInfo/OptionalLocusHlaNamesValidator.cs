using Castle.Core.Internal;
using FluentValidation;
using Nova.Utils.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class OptionalLocusHlaNamesValidator : AbstractValidator<LocusInfo<string>>
    {
        public OptionalLocusHlaNamesValidator()
        {
            RuleFor(x => x.Position1).NotEmpty().When(x => !x.Position2.IsNullOrEmpty());
            RuleFor(x => x.Position2).NotEmpty().When(x => !x.Position1.IsNullOrEmpty());
        }
    }
}