using Atlas.DonorImport.ExternalInterface.Models;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class SearchableDonorUpdateValidator : AbstractValidator<SearchableDonorUpdate>
    {
        public SearchableDonorUpdateValidator()
        {
            RuleFor(x => x.DonorId)
                .NotEmpty();
            
            RuleFor(x => x.IsAvailableForSearch)
                .NotNull();
            
            RuleFor(x => x.SearchableDonorInformation)
                .NotNull()
                .When(x => x.IsAvailableForSearch)
                .SetValidator(new SearchableDonorInformationValidator());

            RuleFor(x => x.DonorId)
                .Equal(x => x.SearchableDonorInformation.DonorId)
                .When(x => x.SearchableDonorInformation != null);
        }
    }
}