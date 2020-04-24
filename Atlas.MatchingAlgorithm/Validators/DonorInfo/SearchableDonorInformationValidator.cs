using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Helpers;
using FluentValidation;
using Nova.DonorService.Client.Models.SearchableDonors;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class SearchableDonorInformationValidator : AbstractValidator<SearchableDonorInformation>
    {
        public SearchableDonorInformationValidator()
        {
            RuleFor(x => x.DonorId).NotNull();
            RuleFor(x => x.DonorType).NotEmpty().Must(DonorInfoHelper.IsValidDonorType);
            RuleFor(x => x.HlaAsPhenotype()).SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}