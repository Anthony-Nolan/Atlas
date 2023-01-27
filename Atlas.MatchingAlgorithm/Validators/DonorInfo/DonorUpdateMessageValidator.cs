using Atlas.Common.ServiceBus.Models;
using Atlas.DonorImport.ExternalInterface.Models;
using FluentValidation;

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