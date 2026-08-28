using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Repositories;

/// <summary>
/// The donor join in DonorSearchRepository.MatchAtLocusSql needs DonorId, DonorType and RegistryCode together.
/// If either index below loses its included columns, SQL Server seeks the index and then does a
/// clustered-index lookup for every candidate donor row - two random reads per row into a table that is tens
/// of GB at live donor volumes - and searches exceed the command timeout.
///
/// These assertions are on the EF model, not on a database, so they need no infrastructure and run in CI. They
/// fail if a model change or a scaffolded migration drops an included column, which is how the shape was lost
/// once before. DonorIndexSchemaTests, in the integration test project, makes the matching check against a
/// real migrated database.
/// </summary>
[TestFixture]
public class DonorIndexModelTests
{
    private IModel model;

    [SetUp]
    public void SetUp()
    {
        // No connection is opened - building the model only runs OnModelCreating.
        var options = new DbContextOptionsBuilder<SearchAlgorithmContext>()
            .UseSqlServer("Server=model-only;Database=model-only")
            .Options;

        using var context = new SearchAlgorithmContext(options);

        // The design-time model, not context.Model: included columns are relational design-time metadata and
        // are stripped from the read-optimised runtime model. This is the model migrations are scaffolded from.
        model = context.GetService<IDesignTimeModel>().Model;
    }

    [TestCase("IX_DonorId", new[] { "DonorId" }, new[] { "DonorType", "RegistryCode" }, null)]
    [TestCase("IX_Donors_DonorType_RegistryCode", new[] { "DonorType", "RegistryCode" }, new[] { "DonorId" }, null)]
    [TestCase("IX_DonorType__DonorId", new[] { "DonorType" }, new[] { "DonorId" }, null)]
    [TestCase("IX_Donors_ExternalDonorCode", new[] { "ExternalDonorCode" }, new string[0], null)]
    [TestCase("FI_DonorIdsWithoutLocusC", new[] { "DonorId" }, new[] { "C_1", "C_2" }, "[C_1] IS NULL AND [C_2] IS NULL")]
    [TestCase("FI_DonorIdsWithoutLocusDQB1", new[] { "DonorId" }, new[] { "DQB1_1", "DQB1_2" }, "[DQB1_1] IS NULL AND [DQB1_2] IS NULL")]
    public void DonorIndex_HasExpectedShape(string indexName, string[] keyColumns, string[] includedColumns, string filter)
    {
        var index = GetDonorIndexes().SingleOrDefault(i => i.GetDatabaseName() == indexName);

        index.Should().NotBeNull($"the model must declare index {indexName} on Donors");
        index.Properties.Select(p => p.Name).Should().Equal(keyColumns);
        (index.GetIncludeProperties() ?? new List<string>()).Should().BeEquivalentTo(includedColumns);
        index.GetFilter().Should().Be(filter);
    }

    /// <summary>
    /// Guards the reason IX_DonorId is declared with the named HasIndex overload. Calling HasIndex more than
    /// once, unnamed, on the same property configures one index rather than several, so an unnamed declaration
    /// would silently overwrite one of the filtered indexes on DonorId instead of adding an index.
    /// </summary>
    [Test]
    public void DonorIndexesOnDonorId_AreDeclaredSeparately()
    {
        var indexesOnDonorIdAlone = GetDonorIndexes()
            .Where(i => i.Properties.Count == 1 && i.Properties.Single().Name == nameof(Donor.DonorId))
            .Select(i => i.GetDatabaseName());

        indexesOnDonorIdAlone.Should().Contain("IX_DonorId");
    }

    /// <summary>
    /// Records which Donors indexes the model owns, so that a scaffolded migration cannot quietly add or drop
    /// one. ATL-310 adopted the last of these into the model, so the model is now a complete description of
    /// the indexes on this table - if this fails because an index is missing, check whether someone has added
    /// one by raw SQL instead of by the model.
    /// </summary>
    [Test]
    public void DonorIndexes_AreTheExpectedSet()
    {
        var indexNames = GetDonorIndexes().Select(i => i.GetDatabaseName());

        indexNames.Should().BeEquivalentTo(
            "IX_DonorId",
            "IX_DonorType__DonorId",
            "IX_Donors_DonorType_RegistryCode",
            "IX_Donors_ExternalDonorCode",
            "FI_DonorIdsWithoutLocusC",
            "FI_DonorIdsWithoutLocusDQB1");
    }

    private IEnumerable<IIndex> GetDonorIndexes() => model.FindEntityType(typeof(Donor)).GetIndexes();
}
