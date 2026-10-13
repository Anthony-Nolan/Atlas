using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

/// <summary>
/// The donor layer, with the value service and the database substituted. The values themselves - one computation per
/// key, the chunks, the stored payloads - are proved in <see cref="SubjectGenotypeSetValueServiceTests"/>.
/// </summary>
[TestFixture]
public class SubjectGenotypeSetPrecomputeServiceTests
{
    private Fixture fixture;
    private string hlaNomenclatureVersion;
    private TransientDatabase database;
    private int nextValueId;

    /// <summary>Each request that the substituted value service gave an id, with the id, in the order of the requests.</summary>
    private List<(SubjectGenotypeSetValueRequest Request, int ValueId)> givenIds;

    /// <summary>The values that the substituted value service fails: by the frequency set of their donor, and their combination.</summary>
    private Dictionary<(HaplotypeFrequencySet, AllowedLociKey), Exception> failures;

    private ISubjectGenotypeSetValueService valueService;
    private ISubjectGenotypeSetRepository repository;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;

    private ISubjectGenotypeSetPrecomputeService precomputeService;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        hlaNomenclatureVersion = fixture.Create<string>();
        database = fixture.Create<TransientDatabase>();
        nextValueId = fixture.Create<int>();
        givenIds = [];
        failures = new Dictionary<(HaplotypeFrequencySet, AllowedLociKey), Exception>();

        valueService = Substitute.For<ISubjectGenotypeSetValueService>();
        // The requests are null only when a test sets up the substitute again: that call is not a request.
        valueService.GetOrComputeValueIds(default, default, default).ReturnsForAnyArgs(callInfo => new SubjectGenotypeSetValueResults(
            callInfo.Arg<IReadOnlyList<SubjectGenotypeSetValueRequest>>()?.Select(OutcomeOf).ToList() ?? [],
            null));

        repository = Substitute.For<ISubjectGenotypeSetRepository>();
        repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
        repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(database).Returns(repository);

        precomputeService = new SubjectGenotypeSetPrecomputeService(valueService, repositoryFactory);
    }

    [Test]
    public async Task Precompute_RequestsTheValuesOfEachDonorAtAllFourCombinations()
    {
        var subjects = NewSubjects(2);

        await precomputeService.Precompute(subjects, hlaNomenclatureVersion, database);

        var requests = RequestedValues();
        requests.Should().HaveCount(subjects.Count * AllowedLociKeyExtensions.All.Count);
        foreach (var subject in subjects)
        {
            var requestsOfDonor = requests.Where(request => request.FrequencySet == subject.FrequencySet).ToList();
            requestsOfDonor.Select(request => request.AllowedLociKey).Should().BeEquivalentTo(AllowedLociKeyExtensions.All);
            requestsOfDonor.Should().OnlyContain(request => request.HlaTyping == subject.HlaTyping);
        }
    }

    [Test]
    public async Task Precompute_GivesTheVersionAndTheDatabaseToTheValueService()
    {
        await precomputeService.Precompute(NewSubjects(1), hlaNomenclatureVersion, database);

        await valueService.Received(1).GetOrComputeValueIds(
            Arg.Any<IReadOnlyList<SubjectGenotypeSetValueRequest>>(),
            hlaNomenclatureVersion,
            database);
    }

    [Test]
    public async Task Precompute_GivesEachDonorTheIdsOfItsOwnFourValues()
    {
        var subjects = NewSubjects(3);

        await precomputeService.Precompute(subjects, hlaNomenclatureVersion, database);

        var expected = givenIds.Select(given => new DonorSubjectGenotypeSetAssignment(
            DonorOf(given.Request, subjects).DonorId,
            given.Request.AllowedLociKey,
            given.ValueId));
        WrittenAssignments().Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task Precompute_WhenOneValueOfADonorFails_WritesTheRowsOfTheOtherDonorsOnly()
    {
        // Three of the four values of the donor have ids, but a donor gets all four rows or none.
        var subjects = NewSubjects(3);
        FailTheValue(subjects[1], fixture.Create<AllowedLociKey>(), new InvalidOperationException(fixture.Create<string>()));

        var act = () => precomputeService.Precompute(subjects, hlaNomenclatureVersion, database);

        await act.Should().ThrowAsync<InvalidOperationException>();
        WrittenAssignments().Select(assignment => assignment.DonorId).Distinct()
            .Should().BeEquivalentTo(new[] { subjects[0].DonorId, subjects[2].DonorId });
        WrittenAssignments().Should().HaveCount(2 * AllowedLociKeyExtensions.All.Count);
    }

    [Test]
    public async Task Precompute_WhenValuesFail_ThrowsTheErrorOfTheFirstValueThatFailed()
    {
        // The caller logs this error, so it must be the real one.
        var subjects = NewSubjects(3);
        var firstFailure = new InvalidOperationException(fixture.Create<string>());
        FailTheValue(subjects[1], fixture.Create<AllowedLociKey>(), firstFailure);
        FailTheValue(subjects[2], fixture.Create<AllowedLociKey>(), new ArgumentException(fixture.Create<string>()));

        var act = () => precomputeService.Precompute(subjects, hlaNomenclatureVersion, database);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(firstFailure);
    }

    [Test]
    public async Task Precompute_WhenTheComputationsStopped_WritesTheRowsOfTheDonorsBeforeTheStop_AndThrowsTheErrorThatStoppedThem()
    {
        var subjects = NewSubjects(2);
        var stoppedBy = new TimeoutException(fixture.Create<string>());
        valueService.GetOrComputeValueIds(default, default, default).ReturnsForAnyArgs(callInfo => new SubjectGenotypeSetValueResults(
            callInfo.Arg<IReadOnlyList<SubjectGenotypeSetValueRequest>>()
                .Select(request => request.FrequencySet == subjects[0].FrequencySet
                    ? new SubjectGenotypeSetValueOutcome(nextValueId++, null)
                    : SubjectGenotypeSetValueOutcome.NotAttempted)
                .ToList(),
            stoppedBy));

        var act = () => precomputeService.Precompute(subjects, hlaNomenclatureVersion, database);

        (await act.Should().ThrowAsync<TimeoutException>()).Which.Should().BeSameAs(stoppedBy);
        WrittenAssignments().Should().HaveCount(AllowedLociKeyExtensions.All.Count)
            .And.OnlyContain(assignment => assignment.DonorId == subjects[0].DonorId);
    }

    [Test]
    public async Task Precompute_WhenNoDonorHasAllItsValues_WritesNoRows()
    {
        var subject = NewSubjects(1).Single();
        FailTheValue(subject, fixture.Create<AllowedLociKey>(), new InvalidOperationException(fixture.Create<string>()));

        var act = () => precomputeService.Precompute([subject], hlaNomenclatureVersion, database);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await repository.DidNotReceiveWithAnyArgs().UpsertDonorAssignments(default);
    }

    [Test]
    public async Task Precompute_WhenAValueHasNoIdAndNoReason_Throws()
    {
        // The value service gives a reason for each missing value. Without one, the donor must not lose its rows silently.
        valueService.GetOrComputeValueIds(default, default, default).ReturnsForAnyArgs(callInfo => new SubjectGenotypeSetValueResults(
            callInfo.Arg<IReadOnlyList<SubjectGenotypeSetValueRequest>>().Select(_ => SubjectGenotypeSetValueOutcome.NotAttempted).ToList(),
            null));

        var act = () => precomputeService.Precompute(NewSubjects(1), hlaNomenclatureVersion, database);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Precompute_WithNoSubjects_TouchesNeitherTheValueServiceNorTheDatabase()
    {
        await precomputeService.Precompute([], hlaNomenclatureVersion, database);

        await valueService.DidNotReceiveWithAnyArgs().GetOrComputeValueIds(default, default, default);
        repositoryFactory.DidNotReceiveWithAnyArgs().GetSubjectGenotypeSetRepositoryForDatabase(default);
    }

    private SubjectGenotypeSetValueOutcome OutcomeOf(SubjectGenotypeSetValueRequest request)
    {
        if (failures.TryGetValue((request.FrequencySet, request.AllowedLociKey), out var failure))
        {
            return new SubjectGenotypeSetValueOutcome(null, new SubjectGenotypeSetValueFailure(PrecomputeErrorKind.Unknown, failure));
        }

        var valueId = nextValueId++;
        givenIds.Add((request, valueId));
        return new SubjectGenotypeSetValueOutcome(valueId, null);
    }

    private void FailTheValue(PrecomputeSubject subject, AllowedLociKey allowedLociKey, Exception failure) =>
        failures[(subject.FrequencySet, allowedLociKey)] = failure;

    /// <summary>The donor of a request. Each donor of <see cref="NewSubjects"/> has a frequency set of its own.</summary>
    private static PrecomputeSubject DonorOf(SubjectGenotypeSetValueRequest request, IEnumerable<PrecomputeSubject> subjects) =>
        subjects.Single(subject => subject.FrequencySet == request.FrequencySet);

    /// <summary>The requests of the single call to the value service.</summary>
    private IReadOnlyList<SubjectGenotypeSetValueRequest> RequestedValues() =>
        (IReadOnlyList<SubjectGenotypeSetValueRequest>) valueService.ReceivedCalls().Single().GetArguments()[0];

    private IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> WrittenAssignments() =>
        repository
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ISubjectGenotypeSetRepository.UpsertDonorAssignments))
            .SelectMany(call => (IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>) call.GetArguments()[0])
            .ToList();

    /// <summary>Donors with a typing and a frequency set of their own.</summary>
    private List<PrecomputeSubject> NewSubjects(int count) =>
        fixture.CreateMany<int>(count)
            .Select(donorId => new PrecomputeSubject(donorId, Typing(), new HaplotypeFrequencySet { Id = fixture.Create<int>() }))
            .ToList();

    private PhenotypeInfo<string> Typing()
    {
        var builder = new PhenotypeInfoBuilder<string>();
        foreach (var locus in new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 })
        {
            builder = builder.WithDataAt(locus, fixture.Create<string>(), fixture.Create<string>());
        }

        return builder.Build();
    }
}
