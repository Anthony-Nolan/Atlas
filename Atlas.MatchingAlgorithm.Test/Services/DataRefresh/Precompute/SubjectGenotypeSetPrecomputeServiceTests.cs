using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
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
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

/// <summary>
/// The orchestrator's own behaviour, with both the database and the imputation pipeline substituted. What is proved
/// here is the sharing - one imputation per distinct key, the same stored id handed to every donor that shares it -
/// which is the whole reason the two tables are shaped as they are.
/// </summary>
[TestFixture]
public class SubjectGenotypeSetPrecomputeServiceTests
{
    private const string HlaNomenclatureVersion = "3500";
    private const int FrequencySetId = 77;

    /// <summary>Ids the substituted repository hands out, high enough not to be confused with a count or an index.</summary>
    private int nextValueId;

    /// <summary>Every id <see cref="ISubjectGenotypeSetRepository.GetOrCreateValueIds"/> handed back, by key.</summary>
    private Dictionary<SubjectGenotypeSetKey, int> createdIds;

    private Fixture fixture;
    private ISubjectGenotypeSetRepository repository;
    private IGenotypeSetService genotypeSetService;

    private ISubjectGenotypeSetPrecomputeService precomputeService;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        nextValueId = 1000;
        createdIds = new Dictionary<SubjectGenotypeSetKey, int>();

        repository = Substitute.For<ISubjectGenotypeSetRepository>();
        genotypeSetService = Substitute.For<IGenotypeSetService>();

        var repositoryFactory = Substitute.For<IDormantRepositoryFactory>();
        repositoryFactory.GetSubjectGenotypeSetRepository().Returns(repository);

        NothingIsStoredYet();
        EveryStoredValueGetsAnId();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(_ => RepresentedSet());

        precomputeService = new SubjectGenotypeSetPrecomputeService(repositoryFactory, genotypeSetService);
    }

    [Test]
    public async Task Precompute_ComputesOneGenotypeSetPerAllowedLociCombination()
    {
        await precomputeService.Precompute([NewSubject()], HlaNomenclatureVersion);

        var requestedLoci = ComputedParameters().Select(parameters => parameters.AllowedLoci).ToList();

        requestedLoci.Should().HaveCount(4);
        requestedLoci.Should().BeEquivalentTo(AllowedLociKeyExtensions.All.Select(key => key.ToLoci()));
    }

    [Test]
    public async Task Precompute_PassesTheMatchingAlgorithmNomenclatureVersionToEveryComputation()
    {
        await precomputeService.Precompute([NewSubject()], HlaNomenclatureVersion);

        ComputedParameters().Should().AllSatisfy(parameters =>
            parameters.MatchingAlgorithmHlaNomenclatureVersion.Should().Be(HlaNomenclatureVersion));
    }

    [Test]
    public async Task Precompute_WritesOneAssignmentPerDonorPerCombination()
    {
        var subjects = new[] { NewSubject(donorId: 1), NewSubject(donorId: 2) };

        await precomputeService.Precompute(subjects, HlaNomenclatureVersion);

        var assignments = WrittenAssignments();

        assignments.Should().HaveCount(8);
        assignments.Select(assignment => (assignment.DonorId, assignment.AllowedLociKey)).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public async Task Precompute_ForTwoDonorsSharingATyping_ComputesOncePerCombination()
    {
        // The "within a batch" half of the de-duplication guarantee, with no database in play: the second donor must
        // not pay for an imputation the first donor's key has already bought.
        var typing = TypedAtEveryLocus();

        await precomputeService.Precompute(
            [NewSubject(donorId: 1, typing: typing), NewSubject(donorId: 2, typing: typing)],
            HlaNomenclatureVersion);

        await genotypeSetService.Received(4).GetGenotypeSet(Arg.Any<SubjectData>(), Arg.Any<MatchPredictionParameters>());
    }

    [Test]
    public async Task Precompute_ForTwoDonorsSharingATyping_AssignsBothToTheSameStoredValue()
    {
        var typing = TypedAtEveryLocus();

        await precomputeService.Precompute(
            [NewSubject(donorId: 1, typing: typing), NewSubject(donorId: 2, typing: typing)],
            HlaNomenclatureVersion);

        var assignments = WrittenAssignments();

        foreach (var allowedLociKey in AllowedLociKeyExtensions.All)
        {
            var idsForCombination = assignments
                .Where(assignment => assignment.AllowedLociKey == allowedLociKey)
                .Select(assignment => assignment.SubjectGenotypeSetValueId);

            idsForCombination.Distinct().Should().ContainSingle($"both donors share their {allowedLociKey} payload");
        }
    }

    [Test]
    public async Task Precompute_ForTwoDonorsWithDifferentTypings_ComputesForBoth()
    {
        var otherTyping = new PhenotypeInfoBuilder<string>(TypedAtEveryLocus()).WithDataAt(Locus.A, "other-1", "other-2").Build();

        await precomputeService.Precompute(
            [NewSubject(donorId: 1), NewSubject(donorId: 2, typing: otherTyping)],
            HlaNomenclatureVersion);

        await genotypeSetService.Received(8).GetGenotypeSet(Arg.Any<SubjectData>(), Arg.Any<MatchPredictionParameters>());
    }

    [Test]
    public async Task Precompute_ForAKeyAlreadyStored_DoesNotComputeItAgain()
    {
        EverythingIsAlreadyStored();

        await precomputeService.Precompute([NewSubject()], HlaNomenclatureVersion);

        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
        await repository.Received(1).GetOrCreateValueIds(Arg.Is<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>(values => values.Count == 0));
    }

    [Test]
    public async Task Precompute_ForAKeyAlreadyStored_AssignsTheDonorToTheStoredValue()
    {
        var storedIds = EverythingIsAlreadyStored();

        await precomputeService.Precompute([NewSubject()], HlaNomenclatureVersion);

        WrittenAssignments().Select(assignment => assignment.SubjectGenotypeSetValueId)
            .Should().BeEquivalentTo(storedIds.Values);
    }

    [Test]
    public async Task Precompute_AssignsDonorsToTheIdsTheRepositoryReturned()
    {
        var subject = NewSubject();

        await precomputeService.Precompute([subject], HlaNomenclatureVersion);

        var keysById = createdIds.ToDictionary(created => created.Value, created => created.Key);

        foreach (var assignment in WrittenAssignments())
        {
            keysById.Should().ContainKey(assignment.SubjectGenotypeSetValueId);
            keysById[assignment.SubjectGenotypeSetValueId].AllowedLociKey.Should().Be(assignment.AllowedLociKey);
            assignment.DonorId.Should().Be(subject.DonorId);
        }
    }

    [Test]
    public async Task Precompute_WritesValuesBeforeAssignments()
    {
        // A crash between the two leaves value rows no donor points at, which the next run finds and reuses. The other
        // order leaves donor rows pointing at ids that do not exist, and the transient databases hold no foreign keys
        // to catch that.
        await precomputeService.Precompute([NewSubject()], HlaNomenclatureVersion);

        Received.InOrder(() =>
        {
            repository.GetOrCreateValueIds(Arg.Any<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>());
            repository.WriteDonorAssignments(Arg.Any<IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>>());
        });
    }

    [Test]
    public async Task Precompute_ForAnUnrepresentedSubject_StoresTheFlagAndNoPayload()
    {
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(_ => new SubjectGenotypeSet(true, [], 0m));

        await precomputeService.Precompute([NewSubject()], HlaNomenclatureVersion);

        StoredValues().Should().AllSatisfy(value =>
        {
            value.IsUnrepresented.Should().BeTrue();
            value.SubjectGenotypeSetData.Should().BeNull();
        });
    }

    [Test]
    public async Task Precompute_ForARepresentedSubject_StoresAPayloadThatDecodesBackToTheComputedSet()
    {
        var computed = RepresentedSet();
        genotypeSetService.GetGenotypeSet(default, default).ReturnsForAnyArgs(_ => computed);

        await precomputeService.Precompute([NewSubject()], HlaNomenclatureVersion);

        StoredValues().Should().AllSatisfy(value =>
        {
            value.IsUnrepresented.Should().BeFalse();

            var decoded = SubjectGenotypeSetPayload.Decode(value.SubjectGenotypeSetData);
            decoded.SumOfLikelihoods.Should().Be(computed.SumOfLikelihoods);
            decoded.Genotypes.Should().HaveCount(computed.Genotypes.Count);
        });
    }

    [Test]
    public async Task Precompute_KeysEachCombinationAgainstTheDonorsFrequencySet()
    {
        await precomputeService.Precompute([NewSubject()], HlaNomenclatureVersion);

        StoredValues().Should().AllSatisfy(value => value.Key.HaplotypeFrequencySetId.Should().Be(FrequencySetId));
    }

    [Test]
    public async Task Precompute_ForDonorsOnDifferentFrequencySets_DoesNotShareAStoredValue()
    {
        // The same typing imputes differently against a different haplotype frequency set, so the set id is part of
        // the key rather than a property of the row.
        var typing = TypedAtEveryLocus();

        await precomputeService.Precompute(
            [
                NewSubject(donorId: 1, typing: typing),
                NewSubject(donorId: 2, typing: typing, frequencySetId: FrequencySetId + 1)
            ],
            HlaNomenclatureVersion);

        StoredValues().Should().HaveCount(8);
    }

    [Test]
    public async Task Precompute_WithNoSubjects_TouchesNeitherTheDatabaseNorThePipeline()
    {
        await precomputeService.Precompute([], HlaNomenclatureVersion);

        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
        await repository.DidNotReceiveWithAnyArgs().GetExistingValueIds(default);
        await repository.DidNotReceiveWithAnyArgs().GetOrCreateValueIds(default);
        await repository.DidNotReceiveWithAnyArgs().WriteDonorAssignments(default);
    }

    #region Substitute setup

    private void NothingIsStoredYet() =>
        repository.GetExistingValueIds(default).ReturnsForAnyArgs(new Dictionary<SubjectGenotypeSetKey, int>());

    /// <returns>The ids the repository will claim are already stored, by key.</returns>
    private IReadOnlyDictionary<SubjectGenotypeSetKey, int> EverythingIsAlreadyStored()
    {
        var storedIds = new Dictionary<SubjectGenotypeSetKey, int>();

        repository.GetExistingValueIds(default).ReturnsForAnyArgs(callInfo =>
        {
            foreach (var key in callInfo.Arg<IReadOnlyCollection<SubjectGenotypeSetKey>>())
            {
                storedIds[key] = nextValueId++;
            }

            return storedIds;
        });

        return storedIds;
    }

    private void EveryStoredValueGetsAnId() =>
        repository.GetOrCreateValueIds(default).ReturnsForAnyArgs(callInfo =>
        {
            var ids = callInfo
                .Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()
                .ToDictionary(value => value.Key, _ => nextValueId++);

            foreach (var (key, id) in ids)
            {
                createdIds[key] = id;
            }

            return ids;
        });

    #endregion

    #region Reading what the substitutes were given

    private IEnumerable<MatchPredictionParameters> ComputedParameters() =>
        genotypeSetService.ReceivedCalls().Select(call => (MatchPredictionParameters) call.GetArguments()[1]);

    private IReadOnlyCollection<SubjectGenotypeSetValueToStore> StoredValues() =>
        (IReadOnlyCollection<SubjectGenotypeSetValueToStore>) repository
            .ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(ISubjectGenotypeSetRepository.GetOrCreateValueIds))
            .GetArguments()[0];

    private IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> WrittenAssignments() =>
        (IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>) repository
            .ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(ISubjectGenotypeSetRepository.WriteDonorAssignments))
            .GetArguments()[0];

    #endregion

    #region Test data

    private static PrecomputeSubject NewSubject(int donorId = 1, PhenotypeInfo<string> typing = null, int frequencySetId = FrequencySetId) =>
        new(donorId, typing ?? TypedAtEveryLocus(), new HaplotypeFrequencySet { Id = frequencySetId });

    private static PhenotypeInfo<string> TypedAtEveryLocus() =>
        new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, "a-1", "a-2")
            .WithDataAt(Locus.B, "b-1", "b-2")
            .WithDataAt(Locus.C, "c-1", "c-2")
            .WithDataAt(Locus.Dqb1, "dqb1-1", "dqb1-2")
            .WithDataAt(Locus.Drb1, "drb1-1", "drb1-2")
            .Build();

    private SubjectGenotypeSet RepresentedSet()
    {
        var genotypes = fixture.CreateMany<decimal>(3)
            .Select(likelihood => new GenotypeAtDesiredResolutions(
                new ImputedGenotype(null, TypedAtEveryLocus(), likelihood),
                TypedAtEveryLocus()))
            .ToList();

        return new SubjectGenotypeSet(false, genotypes, fixture.Create<decimal>());
    }

    #endregion
}
