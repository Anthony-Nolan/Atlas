using FluentValidation;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Helpers;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.DonorProcessing
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