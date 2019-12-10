using FluentValidation;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Helpers;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class DonorInfoValidationFailureEventModel : DonorProcessingFailureEventModel
    {
        private const string MessageName = "Error processing donor - donor validation failure";

        public DonorInfoValidationFailureEventModel(DonorProcessingException<ValidationException> exception) 
            : base(MessageName, exception, exception.FailedDonorInfo)
        {
            Properties.Add("ValidationErrors", exception.Exception.ToErrorMessagesString());
        }
    }
}