using FluentValidation;
using Atlas.MatchingAlgorithm.Models;
using Atlas.Utils.ServiceBus.Models;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class DonorUpdateMessageValidator : AbstractValidator<ServiceBusMessage<SearchableDonorUpdate>>
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