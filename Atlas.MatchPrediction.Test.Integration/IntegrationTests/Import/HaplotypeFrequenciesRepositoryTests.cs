using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Import
{
    [TestFixture]
    public class HaplotypeFrequenciesRepositoryTests
    {
        private Fixture fixture;
        private IHaplotypeFrequenciesRepository repository;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequencyInspectionRepository inspectionRepository;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                repository = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequenciesRepository>();
                setRepository = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencySetRepository>();
                inspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyInspectionRepository>();
            });
        }

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
        }

        /// <summary>
        /// The order of the rows is the tie-break of truncation. The unique index on the names also covers the read, and
        /// its order is the order of the names: so the rows go in, here, in the opposite order of their names.
        /// </summary>
        [Test]
        public async Task GetAllHaplotypeFrequencies_ReturnsTheRowsInTheOrderOfTheirInsert_NotInTheOrderOfTheirNames()
        {
            var set = await setRepository.AddSet(new HaplotypeFrequencySet
            {
                RegistryCode = fixture.Create<string>(),
                PopulationId = fixture.Create<int>(),
                HlaNomenclatureVersion = fixture.Create<string>(),
                Name = fixture.Create<string>(),
                DateTimeAdded = DateTimeOffset.UtcNow
            });
            var frequencies = GenerateHaplotypeFrequencies(set.Id, 100);
            frequencies.Reverse();
            await repository.AddHaplotypeFrequencies(set.Id, frequencies);

            var rows = await repository.GetAllHaplotypeFrequencies(set.Id);

            rows.Select(row => (row.A, row.B, row.C, row.DQB1, row.DRB1))
                .Should().Equal(frequencies.Select(frequency => (frequency.A, frequency.B, frequency.C, frequency.DQB1, frequency.DRB1)));
        }

        /// <summary>
        /// The read of a set uses this connection string, so that a connection string with MARS on does not slow it down.
        /// </summary>
        [Test]
        public void WithoutMultipleActiveResultSets_TurnsOffMultipleActiveResultSets_AndKeepsTheOtherSettings()
        {
            var settings = new SqlConnectionStringBuilder
            {
                DataSource = fixture.Create<string>(),
                InitialCatalog = fixture.Create<string>(),
                UserID = fixture.Create<string>(),
                Password = fixture.Create<string>(),
                ConnectTimeout = fixture.Create<int>(),
                MultipleActiveResultSets = true
            };

            var result = new SqlConnectionStringBuilder(HaplotypeFrequenciesRepository.WithoutMultipleActiveResultSets(settings.ConnectionString));

            result.MultipleActiveResultSets.Should().BeFalse();
            result.DataSource.Should().Be(settings.DataSource);
            result.InitialCatalog.Should().Be(settings.InitialCatalog);
            result.UserID.Should().Be(settings.UserID);
            result.Password.Should().Be(settings.Password);
            result.ConnectTimeout.Should().Be(settings.ConnectTimeout);
        }

        [TestCase(null)]
        [TestCase("")]
        public void WithoutMultipleActiveResultSets_WithNoConnectionString_ReturnsItUnchanged(string connectionString)
        {
            HaplotypeFrequenciesRepository.WithoutMultipleActiveResultSets(connectionString).Should().Be(connectionString);
        }

        [Test]
        public async Task AddHaplotypeFrequencies_WithDuplicateFrequencies_ShouldThrowExceptionAndDoNotLeaveAnyFrequencies()
        {
            const int batchSize = 10000;
            var latestSetId = await inspectionRepository.GetLatestSetId();
            var setId = latestSetId + 1;

            var haplotypeFrequencies = GenerateHaplotypeFrequencies(setId, batchSize);
            var firstEntry = haplotypeFrequencies.First();

            haplotypeFrequencies.Add(new HaplotypeFrequency
            {
                A = firstEntry.A,
                B = firstEntry.B,
                C = firstEntry.C,
                DQB1 = firstEntry.DQB1,
                DRB1 = firstEntry.DRB1,
                SetId = firstEntry.SetId,
                TypingCategory = firstEntry.TypingCategory
            });

            await repository.Invoking(r => r.AddHaplotypeFrequencies(setId, haplotypeFrequencies)).Should().ThrowAsync<Exception>();

            var latestHaplotypeFrequenciesSetId = await inspectionRepository.GetLatestSetId();
            var hasHaplotypeFrequency = await inspectionRepository.HasHaplotypeFrequencies(setId);

            hasHaplotypeFrequency.Should().BeFalse();
            latestHaplotypeFrequenciesSetId.Should().NotBe(setId);
            latestHaplotypeFrequenciesSetId.Should().Be(latestSetId);
        }

        private List<HaplotypeFrequency> GenerateHaplotypeFrequencies(int setId, int numberOfElements)
        {
            const decimal frequency = 0.00001m;
            var lociCollection = new[] { "01:01", "01:02", "01:03", "02:01", "02:02", "02:03", "03:01" };
            var uniqueHaplotypeFrequencies = new List<HaplotypeFrequency>();

            foreach (var a in lociCollection)
                foreach (var b in lociCollection)
                    foreach (var c in lociCollection)
                        foreach (var dqb1 in lociCollection)
                            foreach (var drb1 in lociCollection)
                                uniqueHaplotypeFrequencies.Add(new HaplotypeFrequency 
                                { 
                                    A = a,
                                    B = b,
                                    C = c,
                                    DQB1 = dqb1,
                                    DRB1 = drb1,
                                    SetId = setId,
                                    Frequency = frequency,
                                    TypingCategory = HaplotypeTypingCategory.PGroup
                                });

            return uniqueHaplotypeFrequencies.Take(numberOfElements).ToList();
        }
    }
}
