using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using AwesomeAssertions;
using Azure;
using Azure.Data.Tables;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.MetadataRepositories
{
    /// <summary>
    /// A repository that already existed when another process recreated the dictionary must, once its cache is
    /// invalidated, refill from the NEW table rather than the one it was reading before.
    /// </summary>
    /// <remarks>
    /// Recreation writes a new physical table and repoints the table reference, leaving the old table in place. A
    /// repository that cached its <see cref="TableClient"/> somewhere invalidation cannot reach would miss the cache
    /// after eviction and refill from the OLD table, re-caching stale rows for a further cache lifetime. Here, the
    /// recreation elsewhere is the table reference starting to point at a different table.
    /// </remarks>
    [TestFixture]
    internal class TableClientInvalidationTests
    {
        private const string Version = "3650";
        private const string LookupName = "01:01";
        private const string TableBeforeRecreation = "AlleleNamesBeforeRecreation";
        private const string TableAfterRecreation = "AlleleNamesAfterRecreation";

        private ITableReferenceRepository tableReferenceRepository;
        private IPersistentCacheProvider cacheProvider;
        private AlleleNamesMetadataRepository repository;

        [SetUp]
        public void SetUp()
        {
            tableReferenceRepository = Substitute.For<ITableReferenceRepository>();
            cacheProvider = AppCacheBuilder.NewPersistentCacheProvider();

            var tableBefore = TableHolding("before-recreation");
            var tableAfter = TableHolding("after-recreation");
            var tableFactory = Substitute.For<ITableClientFactory>();
            tableFactory.GetTable(TableBeforeRecreation).Returns(tableBefore);
            tableFactory.GetTable(TableAfterRecreation).Returns(tableAfter);

            repository = new AlleleNamesMetadataRepository(
                tableFactory, tableReferenceRepository, cacheProvider, Substitute.For<IAtlasLogger>());
        }

        [Test]
        public async Task AfterARecreationElsewhereAndInvalidation_AnExistingRepositoryRefillsFromTheNewTable()
        {
            tableReferenceRepository.GetCurrentTableReference(Arg.Any<string>()).Returns(TableBeforeRecreation);
            await repository.LoadDataIntoMemory(Version);
            (await CurrentNamesSeenByRepository()).Should().Equal("before-recreation");

            tableReferenceRepository.GetCurrentTableReference(Arg.Any<string>()).Returns(TableAfterRecreation);
            cacheProvider.RemoveWhere(key => HlaVersionedCacheKey.Matches(key, Version));
            await repository.LoadDataIntoMemory(Version);

            (await CurrentNamesSeenByRepository()).Should().Equal(new[] { "after-recreation" },
                "after invalidation the repository must re-resolve the table reference, not keep reading the table it " +
                "held a client for before the recreation");
        }

        private static TableClient TableHolding(string currentAlleleName)
        {
            var row = new HlaMetadataTableRow(new AlleleNameMetadata(Locus.A, LookupName, new[] { currentAlleleName })).ToTableEntity();

            var table = Substitute.For<TableClient>();
            table.QueryAsync(
                    Arg.Any<Expression<Func<TableEntity, bool>>>(),
                    Arg.Any<int?>(),
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<CancellationToken>())
                .Returns(_ => AsyncPageable<TableEntity>.FromPages(new[]
                {
                    Page<TableEntity>.FromValues(new[] { row }, null, Substitute.For<Response>())
                }));
            return table;
        }

        private async Task<string[]> CurrentNamesSeenByRepository() =>
            (await repository.GetAlleleNameIfExists(Locus.A, LookupName, Version)).CurrentAlleleNames.ToArray();
    }
}
