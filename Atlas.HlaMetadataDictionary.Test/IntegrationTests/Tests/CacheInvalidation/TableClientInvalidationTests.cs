using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using AwesomeAssertions;
using Azure.Data.Tables;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests.CacheInvalidation
{
    /// <summary>
    /// A repository that already existed when another process recreated the dictionary must, once its cache is
    /// invalidated, refill from the NEW table rather than the one it was reading before.
    /// </summary>
    /// <remarks>
    /// Recreation writes a new physical table and repoints the table reference, leaving the old table in place. A
    /// repository that cached its <see cref="TableClient"/> somewhere invalidation cannot reach would miss the cache
    /// after eviction and refill from the OLD table, re-caching stale rows for a further cache lifetime. Two separate
    /// repositories and caches stand in for the two processes. Requires the Azure Storage Emulator (Azurite).
    /// </remarks>
    [TestFixture]
    internal class TableClientInvalidationTests
    {
        private const string ConnectionString = "UseDevelopmentStorage=true";
        private const string LookupName = "01:01";

        /// <summary>Unique per run, so repeated runs never share tables. Digits only: it becomes part of table names.</summary>
        private string version;

        [SetUp]
        public void SetUp() => version = $"9{Random.Shared.Next(100000, 999999)}";

        [TearDown]
        public async Task TearDown()
        {
            var service = new TableServiceClient(ConnectionString);
            await foreach (var table in service.QueryAsync())
            {
                if (table.Name.Contains(version, StringComparison.Ordinal))
                {
                    await service.DeleteTableAsync(table.Name);
                }
            }
        }

        [Test]
        public async Task AfterARecreationElsewhereAndInvalidation_AnExistingRepositoryRefillsFromTheNewTable()
        {
            var recreatingProcess = NewRepository(AppCacheBuilder.NewPersistentCacheProvider());

            var consumerCache = AppCacheBuilder.NewPersistentCacheProvider();
            var consumer = NewRepository(consumerCache);

            await recreatingProcess.RecreateHlaMetadataTable(new[] { AlleleNamed("before-recreation") }, version);
            await consumer.LoadDataIntoMemory(version);
            (await CurrentNamesSeenBy(consumer)).Should().Equal("before-recreation");

            await recreatingProcess.RecreateHlaMetadataTable(new[] { AlleleNamed("after-recreation") }, version);
            consumerCache.RemoveWhere(key => HlaVersionedCacheKey.Matches(key, version));
            await consumer.LoadDataIntoMemory(version);

            (await CurrentNamesSeenBy(consumer)).Should().Equal(new[] { "after-recreation" },
                "after invalidation the consumer must re-resolve the table reference, not keep reading the table it " +
                "held a client for before the recreation");
        }

        private static AlleleNamesMetadataRepository NewRepository(IPersistentCacheProvider cacheProvider)
        {
            var factory = new TableClientFactory(new HlaMetadataDictionarySettings { AzureStorageConnectionString = ConnectionString });
            return new AlleleNamesMetadataRepository(
                factory, new TableReferenceRepository(factory), cacheProvider, Substitute.For<IAtlasLogger>());
        }

        private static AlleleNameMetadata AlleleNamed(string currentName) =>
            new(Locus.A, LookupName, new[] { currentName });

        private async Task<string[]> CurrentNamesSeenBy(IAlleleNamesMetadataRepository repository) =>
            (await repository.GetAlleleNameIfExists(Locus.A, LookupName, version)).CurrentAlleleNames.ToArray();
    }
}
