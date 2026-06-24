using System;
using Atlas.Common.Public.Models.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Models;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;

public static class SearchableDonorUpdateMessageBuilder
{
    private const long DefaultSequenceNumber = 123456;

    public static IPostprocessComposer<DeserializedMessage<SearchableDonorUpdate>> New =>
        FixtureBuilder.For<DeserializedMessage<SearchableDonorUpdate>>()
            .With(x => x.SequenceNumber, DefaultSequenceNumber)
            .With(x => x.LockToken, DonorIdGenerator.NewExternalCode)
            .With(x => x.LockedUntilUtc, DateTime.UtcNow.AddMinutes(5))
            .With(x => x.DeserializedBody, SearchableDonorUpdateBuilder.New.Build());
}