using FluentValidation;
using Nova.DonorService.Client.Models.SearchableDonors;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Helpers;

namespace Nova.SearchAlgorithm.Validators.DonorInfo
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