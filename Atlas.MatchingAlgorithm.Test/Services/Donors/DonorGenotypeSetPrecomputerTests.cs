using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorGenotypeSetPrecomputerTests
    {
        private const TransientDatabase TargetDatabase = TransientDatabase.DatabaseB;
        private const string HlaVersion = "3580";

        private IHaplotypeFrequencyLookupService frequencyLookupService;
        private ISubjectGenotypeSetPrecomputeService precomputeService;
        private IMatchingAlgorithmImportLogger logger;

        private IDonorGenotypeSetPrecomputer precomputer;

        [SetUp]
        public void SetUp()
        {
            frequencyLookupService = Substitute.For<IHaplotypeFrequencyLookupService>();
            frequencyLookupService.GetSingleHaplotypeFrequencySet(default)
                .ReturnsForAnyArgs(call => FrequencySetFor(call.Arg<FrequencySetMetadata>()));

            precomputeService = Substitute.For<ISubjectGenotypeSetPrecomputeService>();
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();

            precomputer = new DonorGenotypeSetPrecomputer(frequencyLookupService, precomputeService, logger);
        }

        [Test]
        public async Task PrecomputeBestEffort_PrecomputesAllDonorsInOneCall_InTargetDatabaseAtGivenVersion()
        {
            var donors = new[] { Donor(1, "reg-1"), Donor(2, "reg-2") };

            await precomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            await precomputeService.Received(1).Precompute(
                Arg.Is<IReadOnlyCollection<PrecomputeSubject>>(s => s.Select(x => x.DonorId).OrderBy(id => id).SequenceEqual(new[] { 1, 2 })),
                HlaVersion,
                TargetDatabase);
        }

        [Test]
        public async Task PrecomputeBestEffort_GivesEachDonorTheFrequencySetOfItsRegistryAndEthnicity()
        {
            var donors = new[] { Donor(1, "reg-1", "eth-1"), Donor(2, "reg-2", "eth-2") };

            await precomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            var subjects = (IReadOnlyCollection<PrecomputeSubject>) precomputeService.ReceivedCalls().Single().GetArguments()[0];
            subjects.Single(s => s.DonorId == 1).FrequencySet.Id.Should().Be(FrequencySetFor("reg-1", "eth-1").Id);
            subjects.Single(s => s.DonorId == 2).FrequencySet.Id.Should().Be(FrequencySetFor("reg-2", "eth-2").Id);
        }

        [Test]
        public async Task PrecomputeBestEffort_LooksUpEachDistinctRegistryAndEthnicityOnce()
        {
            var donors = new[] { Donor(1, "reg-1", "eth-1"), Donor(2, "reg-1", "eth-1"), Donor(3, "reg-1", "eth-2") };

            await precomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            await frequencyLookupService.Received(2).GetSingleHaplotypeFrequencySet(Arg.Any<FrequencySetMetadata>());
        }

        [Test]
        public async Task PrecomputeBestEffort_WhenNothingFails_LogsNoFailure()
        {
            await precomputer.PrecomputeBestEffort(new[] { Donor(1) }, TargetDatabase, HlaVersion);

            logger.DidNotReceiveWithAnyArgs().SendEvent(default, default, default, default);
            logger.DidNotReceiveWithAnyArgs().SendException(default, default, default);
        }

        [Test]
        public async Task PrecomputeBestEffort_WithNoDonors_DoesNothing()
        {
            await precomputer.PrecomputeBestEffort(Array.Empty<DonorInfo>(), TargetDatabase, HlaVersion);

            await precomputeService.DidNotReceiveWithAnyArgs().Precompute(default, default, default);
            await frequencyLookupService.DidNotReceiveWithAnyArgs().GetSingleHaplotypeFrequencySet(default);
        }

        [Test]
        public async Task PrecomputeBestEffort_WhenBatchFails_TriesEachTypingAndFrequencySetGroupOnItsOwn()
        {
            var donors = new[] { Donor(1, typing: Typing("a-1")), Donor(2, typing: Typing("a-1")), Donor(3, typing: Typing("a-2")) };
            FailBatchesLargerThan(2);

            await precomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            var calls = PrecomputeCalls();
            calls.Should().HaveCount(3);
            calls.Skip(1).Select(c => c.Select(s => s.DonorId).OrderBy(id => id).ToArray())
                .Should().BeEquivalentTo(new[] { new[] { 1, 2 }, new[] { 3 } });
        }

        [Test]
        public async Task PrecomputeBestEffort_WhenOneGroupFails_OtherGroupsStillPrecompute_AndOnlyFailedDonorsAreLogged()
        {
            var donors = new[] { Donor(1, typing: Typing("bad")), Donor(2, typing: Typing("good")), Donor(3, typing: Typing("bad")) };
            precomputeService.Precompute(default, default, default).ReturnsForAnyArgs(call =>
                call.Arg<IReadOnlyCollection<PrecomputeSubject>>().Any(s => s.HlaTyping.A.Position1 == "bad")
                    ? Task.FromException(new InvalidOperationException("cannot impute"))
                    : Task.CompletedTask);

            await precomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            PrecomputeCalls().Should().Contain(c => c.Count == 1 && c.Single().DonorId == 2);
            logger.Received(1).SendEvent(
                DonorGenotypeSetPrecomputer.FailureEventName,
                LogLevel.Warn,
                Arg.Is<Dictionary<string, string>>(p =>
                    p["FailedDonorCount"] == "2" &&
                    p["FailedDonorIds"] == "1,3" &&
                    p["TargetDatabase"] == "DatabaseB" &&
                    p["ExceptionType"] == typeof(InvalidOperationException).FullName &&
                    p["ExceptionMessage"] == "cannot impute"),
                Arg.Any<Dictionary<string, double>>());
        }

        [Test]
        public async Task PrecomputeBestEffort_WhenBatchOfOneGroupFails_DoesNotTryTheSameGroupAgain()
        {
            var donors = new[] { Donor(1), Donor(2) };
            precomputeService.Precompute(default, default, default).ThrowsAsyncForAnyArgs(new Exception("boom"));

            await precomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            PrecomputeCalls().Should().HaveCount(1);
            logger.Received(1).SendEvent(
                DonorGenotypeSetPrecomputer.FailureEventName,
                LogLevel.Warn,
                Arg.Is<Dictionary<string, string>>(p => p["FailedDonorCount"] == "2"),
                Arg.Any<Dictionary<string, double>>());
        }

        [Test]
        public async Task PrecomputeBestEffort_WhenFrequencySetLookupFails_SkipsOnlyThoseDonors()
        {
            var donors = new[] { Donor(1, "missing"), Donor(2, "reg-1") };
            frequencyLookupService.GetSingleHaplotypeFrequencySet(Arg.Is<FrequencySetMetadata>(m => m.RegistryCode == "missing"))
                .ThrowsAsync(new Exception("No Global Haplotype frequency set was found"));

            await precomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            PrecomputeCalls().Single().Select(s => s.DonorId).Should().BeEquivalentTo(new[] { 2 });
            logger.Received(1).SendEvent(
                DonorGenotypeSetPrecomputer.FailureEventName,
                LogLevel.Warn,
                Arg.Is<Dictionary<string, string>>(p => p["FailedDonorIds"] == "1"),
                Arg.Any<Dictionary<string, double>>());
        }

        [Test]
        public async Task PrecomputeBestEffort_WhenPrecomputeFails_LogsTheException()
        {
            var exception = new Exception("boom");
            precomputeService.Precompute(default, default, default).ThrowsAsyncForAnyArgs(exception);

            await precomputer.PrecomputeBestEffort(new[] { Donor(1) }, TargetDatabase, HlaVersion);

            logger.Received(1).SendException(exception, LogLevel.Warn, Arg.Any<Dictionary<string, string>>());
        }

        [Test]
        public async Task PrecomputeBestEffort_ListsAtMostMaxLoggedDonorIds_ButCountsAll()
        {
            var donorCount = DonorGenotypeSetPrecomputer.MaxLoggedDonorIds + 5;
            var donors = Enumerable.Range(1, donorCount).Select(id => Donor(id)).ToArray();
            precomputeService.Precompute(default, default, default).ThrowsAsyncForAnyArgs(new Exception("boom"));

            await precomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            logger.Received(1).SendEvent(
                DonorGenotypeSetPrecomputer.FailureEventName,
                LogLevel.Warn,
                Arg.Is<Dictionary<string, string>>(p =>
                    p["FailedDonorCount"] == donorCount.ToString() &&
                    p["FailedDonorIds"].Split(',').Length == DonorGenotypeSetPrecomputer.MaxLoggedDonorIds),
                Arg.Any<Dictionary<string, double>>());
        }

        [Test]
        public async Task PrecomputeBestEffort_NeverThrows_EvenWhenLoggingFails()
        {
            precomputeService.Precompute(default, default, default).ThrowsAsyncForAnyArgs(new Exception("boom"));
            logger.WhenForAnyArgs(l => l.SendEvent(default, default, default, default)).Throw(new Exception("logging down"));

            var act = () => precomputer.PrecomputeBestEffort(new[] { Donor(1) }, TargetDatabase, HlaVersion);

            await act.Should().NotThrowAsync();
        }

        /// <summary>
        /// With the real precompute service: donors that share a typing and a frequency set are imputed once per
        /// locus combination, not once per donor.
        /// </summary>
        [Test]
        public async Task PrecomputeBestEffort_DonorsWithSameTypingAndFrequencySet_AreImputedOncePerLocusCombination()
        {
            var genotypeSetService = Substitute.For<IGenotypeSetService>();
            genotypeSetService.GetGenotypeSet(default, default)
                .ReturnsForAnyArgs(new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0));

            var repository = Substitute.For<ISubjectGenotypeSetRepository>();
            repository.GetExistingValueIds(default).ReturnsForAnyArgs(new Dictionary<SubjectGenotypeSetKey, int>());
            var nextId = 1;
            repository.GetOrCreateValueIds(default).ReturnsForAnyArgs(call =>
                (IReadOnlyDictionary<SubjectGenotypeSetKey, int>) call.Arg<IReadOnlyCollection<SubjectGenotypeSetValueToStore>>()
                    .ToDictionary(v => v.Key, _ => nextId++));
            var repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
            repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(TargetDatabase).Returns(repository);

            var realPrecomputer = new DonorGenotypeSetPrecomputer(
                frequencyLookupService,
                new SubjectGenotypeSetPrecomputeService(
                    new SubjectGenotypeSetValueService(repositoryFactory, genotypeSetService),
                    repositoryFactory),
                logger);

            var donors = new[]
            {
                Donor(1, "reg-1", "eth-1", Typing("a-1")),
                Donor(2, "reg-1", "eth-1", Typing("a-1")),
                Donor(3, "reg-1", "eth-1", Typing("a-1")),
                // Same typing, different frequency set: a different result, so computed separately.
                Donor(4, "reg-2", "eth-1", Typing("a-1")),
            };

            await realPrecomputer.PrecomputeBestEffort(donors, TargetDatabase, HlaVersion);

            const int locusCombinations = 4;
            await genotypeSetService.ReceivedWithAnyArgs(2 * locusCombinations).GetGenotypeSet(default, default);
            await repository.Received(1).UpsertDonorAssignments(
                Arg.Is<IReadOnlyCollection<DonorSubjectGenotypeSetAssignment>>(a => a.Count == donors.Length * locusCombinations));
        }

        private void FailBatchesLargerThan(int size) =>
            precomputeService.Precompute(default, default, default).ReturnsForAnyArgs(call =>
                call.Arg<IReadOnlyCollection<PrecomputeSubject>>().Count > size
                    ? Task.FromException(new Exception("batch failed"))
                    : Task.CompletedTask);

        private List<IReadOnlyCollection<PrecomputeSubject>> PrecomputeCalls() =>
            precomputeService.ReceivedCalls()
                .Select(call => (IReadOnlyCollection<PrecomputeSubject>) call.GetArguments()[0])
                .ToList();

        private static DonorInfo Donor(int donorId, string registryCode = "reg", string ethnicityCode = "eth", PhenotypeInfo<string> typing = null) =>
            new DonorInfoWithExpandedHla
            {
                DonorId = donorId,
                RegistryCode = registryCode,
                EthnicityCode = ethnicityCode,
                HlaNames = typing ?? Typing("a-1")
            };

        private static PhenotypeInfo<string> Typing(string alleleAtA) =>
            new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, alleleAtA, alleleAtA)
                .WithDataAt(Locus.B, "b-1", "b-2")
                .WithDataAt(Locus.C, "c-1", "c-2")
                .WithDataAt(Locus.Dqb1, "dqb1-1", "dqb1-2")
                .WithDataAt(Locus.Drb1, "drb1-1", "drb1-2")
                .Build();

        private static HaplotypeFrequencySet FrequencySetFor(FrequencySetMetadata metadata) =>
            FrequencySetFor(metadata.RegistryCode, metadata.EthnicityCode);

        private static HaplotypeFrequencySet FrequencySetFor(string registryCode, string ethnicityCode) =>
            new() { Id = Math.Abs(HashCode.Combine(registryCode, ethnicityCode)) % 100_000, RegistryCode = registryCode, EthnicityCode = ethnicityCode };
    }
}
