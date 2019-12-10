using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Helpers;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class DonorInfoValidationFailureEventModel : DonorProcessingFailureEventModel
    {
        private const string MessageName = "Error processing donor - donor validation failure";

        public DonorInfoValidationFailureEventModel(
            DonorInfoValidationException exception,
            string donorId) : base(MessageName, exception, donorId)
        {
            Properties.Add("DonorInfo", exception.DonorInfo);
            Properties.Add("ValidationErrors", exception.ValidationException.ToErrorMessagesString());
        }
    }
}