using FluentValidation;
using Nova.DonorService.Client.Models.SearchableDonors;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Extensions;

namespace Nova.SearchAlgorithm.Validators.DonorInfo
{
    public class SearchableDonorInformationValidator : AbstractValidator<SearchableDonorInformation>
    {
        public SearchableDonorInformationValidator()
        {
            RuleFor(x => x.DonorId).NotNull();
            RuleFor(x => x.DonorType).NotNull().IsEnumName(typeof(DonorType));
            RuleFor(x => x.RegistryCode).NotNull().IsEnumName(typeof(RegistryCode));
            RuleFor(x => x.HlaAsPhenotype()).SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}