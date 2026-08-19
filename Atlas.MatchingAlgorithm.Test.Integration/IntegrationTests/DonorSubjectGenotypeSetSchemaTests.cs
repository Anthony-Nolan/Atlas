using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests
{
    /// <summary>
    /// Confirms the unique constraints on the two precompute tables actually reject duplicate inserts at the DB level
    /// — the mechanism the schema relies on to guarantee no duplicate payload storage, rather than caller discipline.
    /// </summary>
    [TestFixture]
    public class DonorSubjectGenotypeSetSchemaTests
    {
        private string transientConnectionString;

        [SetUp]
        public void SetUp()
        {
            var connStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
            transientConnectionString = connStringFactory.GenerateConnectionStringProvider(TransientDatabase.DatabaseA).GetConnectionString();

            DatabaseManager.ClearTransientDatabases();
        }

        [Test]
        public async Task DonorSubjectGenotypeSets_DuplicateDonorIdAndAllowedLociKey_IsRejected()
        {
            await Insert(new DonorSubjectGenotypeSet { DonorId = 1, AllowedLociKey = AllowedLociKey.ABCDrb1Dqb1, SubjectGenotypeSetValueId = 10 });

            // Same (DonorId, AllowedLociKey); differing on a non-key column must not make it a distinct row.
            Func<Task> act = () =>
                Insert(new DonorSubjectGenotypeSet { DonorId = 1, AllowedLociKey = AllowedLociKey.ABCDrb1Dqb1, SubjectGenotypeSetValueId = 20 });

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Test]
        public async Task DonorSubjectGenotypeSets_SameDonorDifferentAllowedLociKey_IsAllowed()
        {
            await Insert(new DonorSubjectGenotypeSet { DonorId = 1, AllowedLociKey = AllowedLociKey.ABCDrb1Dqb1, SubjectGenotypeSetValueId = 10 });

            Func<Task> act = () =>
                Insert(new DonorSubjectGenotypeSet { DonorId = 1, AllowedLociKey = AllowedLociKey.ABDrb1, SubjectGenotypeSetValueId = 20 });

            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task SubjectGenotypeSetValues_DuplicateKey_IsRejected()
        {
            await Insert(NewValue());

            // Same (HlaTypingKey, HaplotypeFrequencySetId, AllowedLociKey).
            Func<Task> act = () => Insert(NewValue());

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Test]
        public async Task SubjectGenotypeSetValues_SameKeyDifferentAllowedLociKey_IsAllowed()
        {
            await Insert(NewValue());

            Func<Task> act = () => Insert(NewValue(allowedLociKey: AllowedLociKey.ABDrb1));

            await act.Should().NotThrowAsync();
        }

        private static SubjectGenotypeSetValue NewValue(AllowedLociKey allowedLociKey = AllowedLociKey.ABCDrb1Dqb1) =>
            new()
            {
                HlaTypingKey = "typing-key",
                HaplotypeFrequencySetId = 1,
                AllowedLociKey = allowedLociKey,
                IsUnrepresented = true,
                SubjectGenotypeSetData = null
            };

        // Fresh context per insert so EF change-tracking never masks a DB-level constraint rejection.
        private async Task Insert<T>(T entity) where T : class
        {
            await using var context = new ContextFactory().Create(transientConnectionString);
            context.Set<T>().Add(entity);
            await context.SaveChangesAsync();
        }
    }
}
