using FluentValidation;
using Nova.SearchAlgorithm.Helpers;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class DonorValidationFailureEventModel : DonorProcessingFailureEventModel
    {
        private const string MessageName = "Error processing donor - donor failed data validation";

        public DonorValidationFailureEventModel(
            ValidationException exception, 
            string donorId) : base(MessageName, exception, donorId)
        {
            Properties.Add("ValidationErrors", exception.ToErrorMessagesString());
        }
    }
}