using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Helpers;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class DonorUpdateFailureEventModel : DonorProcessingFailureEventModel
    {
        private const string MessageName = "Error processing donor - donor update failure";

        public DonorUpdateFailureEventModel(
            DonorUpdateValidationException exception,
            string donorId) : base(MessageName, exception, donorId)
        {
            Properties.Add("DonorUpdate", exception.DonorUpdate);
            Properties.Add("ValidationErrors", exception.ValidationException.ToErrorMessagesString());
        }
    }
}