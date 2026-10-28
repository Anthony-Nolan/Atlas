using Atlas.MatchPrediction.Data.Repositories;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Repositories;

[TestFixture]
public class HaplotypeFrequenciesRepositoryTests
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
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
}
