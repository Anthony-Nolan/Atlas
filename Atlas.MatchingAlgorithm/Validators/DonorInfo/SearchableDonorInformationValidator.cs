using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Extensions;
using FluentValidation;
using Atlas.MatchingAlgorithm.Models;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class SearchableDonorInformationValidator : AbstractValidator<SearchableDonorInformation>
    {
        public SearchableDonorInformationValidator()
        {
            RuleFor(x => x.DonorId).NotNull();
            RuleFor(x => x.DonorType).NotEmpty().DependentRules(() => {
                RuleFor(x => x.DonorType).Must(typeString => typeString.TryParseStringValueToEnum<DonorType>(out _));
            });
            RuleFor(x => x.HlaAsPhenotype()).SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}