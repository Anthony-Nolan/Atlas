using FluentValidation;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Helpers;

namespace Nova.SearchAlgorithm.ApplicationInsights.DonorProcessing
{
    public class DonorInfoValidationFailureEventModel : DonorProcessingFailureEventModel
    {
        public DonorInfoValidationFailureEventModel(string eventName, DonorProcessingException<ValidationException> exception) 
            : base(eventName, exception, exception.FailedDonorInfo)
        {
            Properties.Add("ValidationErrors", exception.Exception.ToErrorMessagesString());
        }
    }
}