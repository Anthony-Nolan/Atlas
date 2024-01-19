using System;
using Atlas.Common.Public.Models.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Models;
using LochNessBuilder;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    [Builder]
    public static class SearchableDonorUpdateMessageBuilder
    {
        private const long DefaultSequenceNumber = 123456;

        public static Builder<ServiceBusMessage<SearchableDonorUpdate>> New =>
            Builder<ServiceBusMessage<SearchableDonorUpdate>>.New
                .With(x => x.SequenceNumber, DefaultSequenceNumber)
                .With(x => x.LockToken, DonorIdGenerator.NewExternalCode)
                .With(x => x.LockedUntilUtc, DateTime.UtcNow.AddMinutes(5))
                .With(x => x.DeserializedBody, SearchableDonorUpdateBuilder.New);
    }
}
