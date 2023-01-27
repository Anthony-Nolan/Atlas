using Atlas.Common.Validation;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Models;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class SearchableDonorInformationValidator : AbstractValidator<SearchableDonorInformation>
    {
        public SearchableDonorInformationValidator()
        {
            RuleFor(x => x.DonorId).NotNull();
            RuleFor(x => x.HlaAsPhenotypeInfoTransfer()).SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}