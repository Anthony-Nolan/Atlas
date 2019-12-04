using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.Utils.ServiceBus.Models;

namespace Nova.SearchAlgorithm.Validators.DonorUpdates
{
    public class DonorUpdateMessageValidator : AbstractValidator<ServiceBusMessage<SearchableDonorUpdateModel>>
    {
        public DonorUpdateMessageValidator()
        {
            RuleFor(x => x.SequenceNumber).NotNull();
            RuleFor(x => x.DeserializedBody).NotNull().SetValidator(new SearchableDonorUpdateValidator());
        }
    }
}