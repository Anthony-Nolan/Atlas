using FluentValidation;
using Nova.DonorService.Client.Models.SearchableDonors;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Helpers;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class SearchableDonorInformationValidator : AbstractValidator<SearchableDonorInformation>
    {
        public SearchableDonorInformationValidator()
        {
            RuleFor(x => x.DonorId).NotNull();
            RuleFor(x => x.DonorType).NotEmpty().Must(DonorInfoHelper.IsValidDonorType);
            RuleFor(x => x.RegistryCode).NotEmpty().IsEnumName(typeof(RegistryCode));
            RuleFor(x => x.HlaAsPhenotype()).SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}