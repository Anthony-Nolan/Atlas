using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.Utils.ServiceBus.Models;

namespace Nova.SearchAlgorithm.Exceptions
{
    public class DonorUpdateValidationException : DonorInfoValidationException
    {
        public DonorUpdateValidationException(
            ServiceBusMessage<SearchableDonorUpdateModel> info,
            ValidationException exception)
            : base(info, exception)
        {
        }
    }
}