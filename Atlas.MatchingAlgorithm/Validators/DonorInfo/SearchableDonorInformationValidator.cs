using Atlas.Common.Validation;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Models;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    /// <remarks>
    /// The data refresh donor import runs these rules tens of millions of times per refresh, and does so via
    /// <see cref="SearchableDonorInformationFastValidator"/> instead, for cost reasons. Any rule changed or added here must be mirrored
    /// there; <c>SearchableDonorInformationFastValidatorTests</c> enforces that the two stay equivalent.
    /// </remarks>
    public class SearchableDonorInformationValidator : AbstractValidator<SearchableDonorInformation>
    {
        public SearchableDonorInformationValidator()
        {
            RuleFor(x => x.DonorId).NotNull();
            RuleFor(x => x.HlaAsPhenotypeInfoTransfer()).SetValidator(new PhenotypeHlaNamesValidator());
        }
    }
}