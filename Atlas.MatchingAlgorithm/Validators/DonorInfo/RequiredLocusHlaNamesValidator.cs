using FluentValidation;
using Nova.Utils.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class RequiredLocusHlaNamesValidator : AbstractValidator<LocusInfo<string>>
    {
        public RequiredLocusHlaNamesValidator()
        {
            RuleFor(x => x.Position1).NotEmpty();
            RuleFor(x => x.Position2).NotEmpty();
        }
    }
}