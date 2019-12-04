using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;

namespace Nova.SearchAlgorithm.Validators.DonorUpdates
{
    public class SearchableDonorUpdateValidator : AbstractValidator<SearchableDonorUpdateModel>
    {
        public SearchableDonorUpdateValidator()
        {
            RuleFor(x => x.DonorId)
                .NotEmpty()
                .Must(x => int.TryParse(x, out var ignored));
            
            RuleFor(x => x.IsAvailableForSearch)
                .NotNull();
            
            RuleFor(x => x.SearchableDonorInformation)
                .NotNull()
                .When(x => x.IsAvailableForSearch)
                .SetValidator(new SearchableDonorInformationValidator());

            RuleFor(x => x.DonorId)
                .Equal(x => x.SearchableDonorInformation.DonorId.ToString())
                .When(x => x.SearchableDonorInformation != null);
        }
    }
}