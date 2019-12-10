using FluentValidation;
using Nova.DonorService.Client.Models.SearchableDonors;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class SearchableDonorInfoValidationException : DonorInfoValidationException
    {
        public SearchableDonorInfoValidationException(
            SearchableDonorInformation info,
            ValidationException exception)
            : base(info, exception)
        {
        }
    }
}