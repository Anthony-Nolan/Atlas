using FluentValidation;
using Nova.DonorService.Client.Models.SearchableDonors;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Validators.DonorUpdates
{
    public class SearchableDonorInformationValidator : AbstractValidator<SearchableDonorInformation>
    {
        public SearchableDonorInformationValidator()
        {
            RuleFor(x => x.DonorId).NotNull();
            RuleFor(x => x.DonorType).NotNull().IsEnumName(typeof(DonorType));
            RuleFor(x => x.RegistryCode).NotNull().IsEnumName(typeof(RegistryCode));

            // Donor's HLA info will be validated elsewhere
        }
    }
}