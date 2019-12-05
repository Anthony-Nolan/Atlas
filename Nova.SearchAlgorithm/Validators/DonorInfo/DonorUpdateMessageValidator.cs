using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.Utils.ServiceBus.Models;

namespace Nova.SearchAlgorithm.Validators.DonorInfo
{
    public class DonorUpdateMessageValidator : AbstractValidator<ServiceBusMessage<SearchableDonorUpdateModel>>
    {
        public DonorUpdateMessageValidator()
        {
            RuleFor(x => x.SequenceNumber).NotNull();
            RuleFor(x => x.LockToken).NotEmpty();
            RuleFor(x => x.LockedUntilUtc).NotNull();
            RuleFor(x => x.DeserializedBody).NotNull().SetValidator(new SearchableDonorUpdateValidator());
        }
    }
}