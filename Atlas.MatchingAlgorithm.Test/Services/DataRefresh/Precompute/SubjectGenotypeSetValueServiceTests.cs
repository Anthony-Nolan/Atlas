using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

[TestFixture]
public class SubjectGenotypeSetValueServiceTests
{
    private Fixture fixture;
    private string hlaNomenclatureVersion;
    private TransientDatabase database;
    private int nextValueId;

    private ISubjectGenotypeSetRepository repository;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private IGenotypeSetService genotypeSetService;

    private ISubjectGenotypeSetValueService valueService;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        hlaNomenclatureVersion = fixture.Create<string>();
        database = fixture.Create<TransientDatabase>();
        nextValueId = fixture.Create<int>();

        repository = Substitute.For<ISubjectGenotypeSetRepository>();
        repository.GetExistingValueIds(default).ReturnsForAnyArgs(new Dictionary<SubjectGenotypeSetKey, int>());
        // The values are null only when a test sets up the store again: that call is not a store.
        repository.GetOrCreateValueIds(default).ReturnsForAnyArgs(callInfo =>
            callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()?.ToDictionary(value => value.Key, _ => nextValueId++)
            ?? new Dictionary<SubjectGenotypeSetKey, int>());

        repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
        repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(database).Returns(repository);

        genotypeSetService = Substitute.For<IGenotypeSetService>();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(_ => new SubjectGenotypeSet(true, [], 0m));

        valueService = new SubjectGenotypeSetValueService(repositoryFactory, genotypeSetService);
    }

    [Test]
    public async Task GetOrComputeValueIds_ComputesAndStoresEachValue_AndReturnsItsId()
    {
        var requests = NewRequests(3);

        var results = await valueService.GetOrComputeValueIds(requests, hlaNomenclatureVersion, database);

        results.StoppedBy.Should().BeNull();
        results.Outcomes.Should().HaveCount(3).And.OnlyContain(outcome => outcome.ValueId != null && outcome.Failure == null);
        results.Outcomes.Select(outcome => outcome.ValueId).Should().OnlyHaveUniqueItems();
        await genotypeSetService.Received(3).GetGenotypeSet(Arg.Any<SubjectData>(), Arg.Any<MatchPredictionParameters>());
    }

    [Test]
    public async Task GetOrComputeValueIds_ForAValueThatIsStored_ReturnsItsIdAndComputesNothing()
    {
        // A value that an earlier batch stored, such as the same typing of another registry with the same frequency set.
        var request = NewRequest();
        var storedId = fixture.Create<int>();
        repository.GetExistingValueIds(default).ReturnsForAnyArgs(callInfo =>
            callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetKey>>().ToDictionary(key => key, _ => storedId));

        var results = await valueService.GetOrComputeValueIds([request], hlaNomenclatureVersion, database);

        results.Outcomes.Should().ContainSingle().Which.ValueId.Should().Be(storedId);
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
    }

    [Test]
    public async Task GetOrComputeValueIds_ForTwoRequestsWithOneKey_ComputesOnce_AndGivesBothTheId()
    {
        // Two groups of different registries whose pairs resolve to one frequency set.
        var request = NewRequest();
        var sameKey = request with { SubjectLogDescription = fixture.Create<string>() };

        var results = await valueService.GetOrComputeValueIds([request, sameKey], hlaNomenclatureVersion, database);

        await genotypeSetService.Received(1).GetGenotypeSet(Arg.Any<SubjectData>(), Arg.Any<MatchPredictionParameters>());
        results.Outcomes.Select(outcome => outcome.ValueId).Distinct().Should().ContainSingle().Which.Should().NotBeNull();
    }

    [Test]
    public async Task GetOrComputeValueIds_ForTwoRequestsThatDifferOnlyByEmptyOrNull_ComputesOnce_WithNull()
    {
        // The request with the empty name comes first. Without the change to null, its typing is the one imputed, and the
        // HLA Metadata Dictionary throws for it: whether a batch fails would then depend on the order of its donors.
        var withEmpty = NewRequest(new PhenotypeInfoBuilder<string>(Typing()).WithDataAt(Locus.C, string.Empty, string.Empty).Build());
        var withNull = withEmpty with
        {
            HlaTyping = new PhenotypeInfoBuilder<string>(withEmpty.HlaTyping).WithDataAt(Locus.C, null, null).Build()
        };

        var results = await valueService.GetOrComputeValueIds([withEmpty, withNull], hlaNomenclatureVersion, database);

        await genotypeSetService.Received(1).GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(withNull.HlaTyping)),
            Arg.Any<MatchPredictionParameters>());
        results.Outcomes.Select(outcome => outcome.ValueId).Distinct().Should().ContainSingle().Which.Should().NotBeNull();
    }

    [Test]
    public async Task GetOrComputeValueIds_ForOneTypingOnTwoFrequencySets_StoresAValueForEachSet()
    {
        // The same typing imputes differently against another frequency set, so the set is part of the key.
        var request = NewRequest();
        var onOtherSet = request with { FrequencySet = new HaplotypeFrequencySet { Id = fixture.Create<int>() } };
        IReadOnlyCollection<SubjectGenotypeSetValueToStore> stored = null;
        repository.When(r => r.GetOrCreateValueIds(Arg.Any<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()))
            .Do(callInfo => stored = callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>());

        await valueService.GetOrComputeValueIds([request, onOtherSet], hlaNomenclatureVersion, database);

        stored.Select(value => value.Key.HaplotypeFrequencySetId)
            .Should().BeEquivalentTo(new[] { request.FrequencySet.Id, onOtherSet.FrequencySet.Id });
    }

    [Test]
    public async Task GetOrComputeValueIds_ImputesAgainstTheFrequencySet_AtTheLociOfTheKey_AndTheVersion()
    {
        var request = NewRequest();

        await valueService.GetOrComputeValueIds([request], hlaNomenclatureVersion, database);

        await genotypeSetService.Received(1).GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.SubjectFrequencySet.FrequencySet == request.FrequencySet),
            Arg.Is<MatchPredictionParameters>(parameters =>
                parameters.AllowedLoci.SetEquals(request.AllowedLociKey.ToLoci())
                && parameters.MatchingAlgorithmHlaNomenclatureVersion == hlaNomenclatureVersion));
    }

    [Test]
    public async Task GetOrComputeValueIds_ForAnEmptyHlaName_ImputesItAsNull()
    {
        // The HLA Metadata Dictionary throws for an empty name. The key already reads empty and null as one.
        var typing = new PhenotypeInfoBuilder<string>(Typing()).WithDataAt(Locus.C, string.Empty, string.Empty).Build();
        var expected = new PhenotypeInfoBuilder<string>(typing).WithDataAt(Locus.C, null, null).Build();

        await valueService.GetOrComputeValueIds([NewRequest(typing)], hlaNomenclatureVersion, database);

        await genotypeSetService.Received(1).GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(expected)),
            Arg.Any<MatchPredictionParameters>());
    }

    [Test]
    public async Task GetOrComputeValueIds_UsesTheDatabaseThatItIsGiven()
    {
        await valueService.GetOrComputeValueIds([NewRequest()], hlaNomenclatureVersion, database);

        repositoryFactory.Received().GetSubjectGenotypeSetRepositoryForDatabase(database);
        repositoryFactory.DidNotReceive().GetSubjectGenotypeSetRepositoryForDatabase(database.Other());
    }

    [Test]
    public async Task GetOrComputeValueIds_WhenAComputationFailsPermanently_FailsOnlyItsValue_AndGoesOn()
    {
        // A bad typing must not cost the other groups of its batch their values.
        var requests = NewRequests(3);
        var failure = new HlaMetadataDictionaryException(fixture.Create<string>(), fixture.Create<string>(), fixture.Create<string>());
        GivenTheComputationOf(requests[1]).ThrowsAsync(failure);

        var results = await valueService.GetOrComputeValueIds(requests, hlaNomenclatureVersion, database);

        results.StoppedBy.Should().BeNull();
        results.Outcomes[1].Should().Be(new SubjectGenotypeSetValueOutcome(null, new SubjectGenotypeSetValueFailure(PrecomputeErrorKind.KnownPermanent, failure)));
        results.Outcomes[0].ValueId.Should().NotBeNull();
        results.Outcomes[2].ValueId.Should().NotBeNull();
    }

    [Test]
    public async Task GetOrComputeValueIds_WhenAComputationFailsWithAnUnknownError_FailsOnlyItsValue_AndGoesOn()
    {
        var requests = NewRequests(3);
        var failure = new InvalidOperationException(fixture.Create<string>());
        GivenTheComputationOf(requests[0]).ThrowsAsync(failure);

        var results = await valueService.GetOrComputeValueIds(requests, hlaNomenclatureVersion, database);

        results.StoppedBy.Should().BeNull();
        results.Outcomes[0].Failure.Should().Be(new SubjectGenotypeSetValueFailure(PrecomputeErrorKind.Unknown, failure));
        results.Outcomes.Skip(1).Should().OnlyContain(outcome => outcome.ValueId != null);
    }

    [Test]
    public async Task GetOrComputeValueIds_AtAKnownTemporaryError_Stops_AndStoresTheValuesComputedBeforeIt()
    {
        // The values after it would fail too, and the values before it are done work that a retry need not repeat.
        var requests = NewRequests(3);
        var failure = new TimeoutException(fixture.Create<string>());
        GivenTheComputationOf(requests[1]).ThrowsAsync(failure);

        var results = await valueService.GetOrComputeValueIds(requests, hlaNomenclatureVersion, database);

        results.StoppedBy.Should().BeSameAs(failure);
        results.Outcomes[0].ValueId.Should().NotBeNull();
        results.Outcomes[1].Failure.Should().Be(new SubjectGenotypeSetValueFailure(PrecomputeErrorKind.KnownTemporary, failure));
        results.Outcomes[2].Should().Be(new SubjectGenotypeSetValueOutcome(null, null));
        await genotypeSetService.Received(2).GetGenotypeSet(Arg.Any<SubjectData>(), Arg.Any<MatchPredictionParameters>());
    }

    [Test]
    public async Task GetOrComputeValueIds_WhenTheStoreFails_StopsWithItsError_AndLosesTheValuesOfTheChunk()
    {
        var requests = NewRequests(2);
        var failure = new InvalidOperationException(fixture.Create<string>());
        repository.GetOrCreateValueIds(default).ThrowsAsyncForAnyArgs(failure);

        var results = await valueService.GetOrComputeValueIds(requests, hlaNomenclatureVersion, database);

        results.StoppedBy.Should().BeSameAs(failure);
        results.Outcomes.Should().OnlyContain(outcome => outcome.ValueId == null && outcome.Failure == null);
    }

    [Test]
    public async Task GetOrComputeValueIds_WhenTheReadFails_StopsBeforeItComputes()
    {
        var failure = new InvalidOperationException(fixture.Create<string>());
        repository.GetExistingValueIds(default).ThrowsAsyncForAnyArgs(failure);

        var results = await valueService.GetOrComputeValueIds(NewRequests(2), hlaNomenclatureVersion, database);

        results.StoppedBy.Should().BeSameAs(failure);
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
    }

    [Test]
    public async Task GetOrComputeValueIds_ForMoreValuesThanOneChunk_StoresEachChunkBeforeItComputesTheNext()
    {
        // Only one chunk of payloads is held at a time, and a stop loses only the chunk in hand.
        var requests = NewRequests(SubjectGenotypeSetValueService.ValueChunkSize + 1);
        var computationsAtEachStore = new List<int>();
        repository
            .When(r => r.GetOrCreateValueIds(Arg.Any<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()))
            .Do(_ => computationsAtEachStore.Add(genotypeSetService.ReceivedCalls().Count()));

        await valueService.GetOrComputeValueIds(requests, hlaNomenclatureVersion, database);

        computationsAtEachStore.Should().Equal(SubjectGenotypeSetValueService.ValueChunkSize, requests.Count);
    }

    [Test]
    public async Task GetOrComputeValueIds_ForMoreValuesThanOneChunk_ReturnsTheIdOfEveryValue()
    {
        var requests = NewRequests(SubjectGenotypeSetValueService.ValueChunkSize + 1);

        var results = await valueService.GetOrComputeValueIds(requests, hlaNomenclatureVersion, database);

        results.Outcomes.Should().HaveCount(requests.Count).And.OnlyContain(outcome => outcome.ValueId != null);
        results.Outcomes.Select(outcome => outcome.ValueId).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public async Task GetOrComputeValueIds_ForAnUnrepresentedSubject_StoresTheFlagAndNoPayload()
    {
        IReadOnlyCollection<SubjectGenotypeSetValueToStore> stored = null;
        repository.When(r => r.GetOrCreateValueIds(Arg.Any<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()))
            .Do(callInfo => stored = callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>());

        await valueService.GetOrComputeValueIds([NewRequest()], hlaNomenclatureVersion, database);

        stored.Should().ContainSingle().Which.Should().Match<SubjectGenotypeSetValueToStore>(value =>
            value.IsUnrepresented && value.SubjectGenotypeSetData == null);
    }

    [Test]
    public async Task GetOrComputeValueIds_ForARepresentedSubject_StoresAPayloadThatDecodesToTheComputedSet()
    {
        var computed = RepresentedSet();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(computed);
        IReadOnlyCollection<SubjectGenotypeSetValueToStore> stored = null;
        repository.When(r => r.GetOrCreateValueIds(Arg.Any<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()))
            .Do(callInfo => stored = callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>());

        await valueService.GetOrComputeValueIds([NewRequest()], hlaNomenclatureVersion, database);

        var value = stored.Should().ContainSingle().Which;
        value.IsUnrepresented.Should().BeFalse();
        var decoded = SubjectGenotypeSetPayload.Decode(value.SubjectGenotypeSetData);
        decoded.SumOfLikelihoods.Should().Be(computed.SumOfLikelihoods);
        decoded.Genotypes.Should().HaveCount(computed.Genotypes.Count);
    }

    [Test]
    public async Task GetOrComputeValueIds_WithNoRequests_TouchesNeitherTheDatabaseNorThePipeline()
    {
        var results = await valueService.GetOrComputeValueIds([], hlaNomenclatureVersion, database);

        results.Outcomes.Should().BeEmpty();
        repositoryFactory.DidNotReceiveWithAnyArgs().GetSubjectGenotypeSetRepositoryForDatabase(default);
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
    }

    /// <summary>The computation of the request, to set up: its subject has the typing of the request.</summary>
    private Task<SubjectGenotypeSet> GivenTheComputationOf(SubjectGenotypeSetValueRequest request) =>
        genotypeSetService.GetGenotypeSet(
            Arg.Is<SubjectData>(subject => subject.HlaTyping.Equals(request.HlaTyping)),
            Arg.Any<MatchPredictionParameters>());

    private List<SubjectGenotypeSetValueRequest> NewRequests(int count) =>
        Enumerable.Range(0, count).Select(_ => NewRequest()).ToList();

    /// <summary>A request with a typing of its own, so its key is its own.</summary>
    private SubjectGenotypeSetValueRequest NewRequest(PhenotypeInfo<string> typing = null) => new(
        typing ?? Typing(),
        fixture.Create<AllowedLociKey>(),
        new HaplotypeFrequencySet { Id = fixture.Create<int>() },
        fixture.Create<string>());

    private PhenotypeInfo<string> Typing()
    {
        var builder = new PhenotypeInfoBuilder<string>();
        foreach (var locus in new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 })
        {
            builder = builder.WithDataAt(locus, fixture.Create<string>(), fixture.Create<string>());
        }

        return builder.Build();
    }

    private SubjectGenotypeSet RepresentedSet()
    {
        var genotypes = fixture.CreateMany<decimal>(3)
            .Select(likelihood => new GenotypeAtDesiredResolutions(new ImputedGenotype(null, Typing(), likelihood), Typing()))
            .ToList();

        return new SubjectGenotypeSet(false, genotypes, fixture.Create<decimal>());
    }
}
