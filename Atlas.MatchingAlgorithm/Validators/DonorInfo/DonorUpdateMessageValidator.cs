using Atlas.Common.Public.Models.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Models;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo
{
    public class DonorUpdateMessageValidator : AbstractValidator<DeserializedMessage<SearchableDonorUpdate>>
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