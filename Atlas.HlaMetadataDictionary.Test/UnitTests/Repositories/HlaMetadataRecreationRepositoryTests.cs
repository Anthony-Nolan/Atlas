using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.InternalModels;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AwesomeAssertions;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories
{
    [TestFixture]
    internal class HlaMetadataRecreationRepositoryTests
    {
        private const string Version = "3650";

        private TableClient table;
        private HlaMetadataRecreationRepository repository;

        [SetUp]
        public void SetUp()
        {
            table = Substitute.For<TableClient>();
            var factory = Substitute.For<ITableClientFactory>();
            factory.GetTable(Arg.Any<string>()).Returns(table);

            repository = new HlaMetadataRecreationRepository(factory);
        }

        [Test]
        public async Task WasWrittenByThisProcess_ForTheStampItRecorded_IsTrue()
        {
            HlaMetadataRecreationRow written = null;
            table.UpsertEntityAsync(Arg.Do<HlaMetadataRecreationRow>(row => written = row), Arg.Any<TableUpdateMode>(), Arg.Any<CancellationToken>());

            await repository.RecordRecreation(Version);

            repository.WasWrittenByThisProcess(Version, written.Stamp).Should().BeTrue();
        }

        [Test]
        public async Task WasWrittenByThisProcess_ForAStampWrittenElsewhere_IsFalse()
        {
            await repository.RecordRecreation(Version);

            repository.WasWrittenByThisProcess(Version, Guid.NewGuid().ToString()).Should().BeFalse();
        }

        [Test]
        public void WasWrittenByThisProcess_ForAVersionThisProcessNeverRecorded_IsFalse()
        {
            repository.WasWrittenByThisProcess(Version, Guid.NewGuid().ToString()).Should().BeFalse();
        }

        /// <summary>
        /// Otherwise a poll landing between the write and the remembering would take this process's own stamp for
        /// another process's, and evict again.
        /// </summary>
        [Test]
        public async Task RecordRecreation_RemembersTheStampBeforeWritingIt()
        {
            var rememberedWhenWritten = false;
            table.UpsertEntityAsync(
                Arg.Do<HlaMetadataRecreationRow>(row => rememberedWhenWritten = repository.WasWrittenByThisProcess(Version, row.Stamp)),
                Arg.Any<TableUpdateMode>(),
                Arg.Any<CancellationToken>());

            await repository.RecordRecreation(Version);

            rememberedWhenWritten.Should().BeTrue();
        }

        /// <summary>
        /// The stamps this process wrote are remembered in memory, and the watcher resolves the repository in a scope
        /// of its own - a scoped registration would give it an instance that remembers nothing.
        /// </summary>
        [Test]
        public void IsRegisteredAsASingleton()
        {
            var services = new ServiceCollection();
            services.RegisterHlaMetadataDictionary(
                _ => new HlaMetadataDictionarySettings(),
                _ => new ApplicationInsightsSettings(),
                _ => new MacDictionarySettings());

            services.Single(d => d.ServiceType == typeof(IHlaMetadataRecreationRepository)).Lifetime
                .Should().Be(ServiceLifetime.Singleton);
        }
    }
}
