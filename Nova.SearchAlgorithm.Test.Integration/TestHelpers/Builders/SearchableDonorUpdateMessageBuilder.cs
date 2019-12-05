using LochNessBuilder;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.Utils.ServiceBus.Models;
using System;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    [Builder]
    public static class SearchableDonorUpdateMessageBuilder
    {
        private const long DefaultSequenceNumber = 123456;

        public static Builder<ServiceBusMessage<SearchableDonorUpdateModel>> New =>
            Builder<ServiceBusMessage<SearchableDonorUpdateModel>>.New
                .With(x => x.SequenceNumber, DefaultSequenceNumber)
                .With(x => x.LockToken, Guid.NewGuid().ToString())
                .With(x => x.LockedUntilUtc, DateTime.UtcNow.AddMinutes(5))
                .With(x => x.DeserializedBody, SearchableDonorUpdateBuilder.New);
    }
}
