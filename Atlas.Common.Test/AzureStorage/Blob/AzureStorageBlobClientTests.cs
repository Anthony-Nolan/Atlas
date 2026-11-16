using System;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.AzureStorage.Blob;

[TestFixture]
public class AzureStorageBlobClientTests
{
    private Fixture fixture;
    private IAtlasLogger logger;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        logger = Substitute.For<IAtlasLogger>();
    }

    [Test]
    public void Constructor_WithSameConnectionString_SharesOneBlobServiceClient()
    {
        var connectionString = ConnectionString();

        var first = new BlobDownloader(connectionString, logger);
        var second = new BlobDownloader(connectionString, logger);

        second.ServiceClient.Should().BeSameAs(first.ServiceClient);
    }

    [Test]
    public void Constructor_WithDifferentConnectionStrings_UsesDifferentBlobServiceClients()
    {
        var first = new BlobDownloader(ConnectionString(), logger);
        var second = new BlobDownloader(ConnectionString(), logger);

        second.ServiceClient.Should().NotBeSameAs(first.ServiceClient);
    }

    [Test]
    public void Constructor_ForDownloaderAndUploaderWithSameConnectionString_SharesOneBlobServiceClient()
    {
        var connectionString = ConnectionString();

        var downloader = new BlobDownloader(connectionString, logger);
        var uploader = new BlobUploader(connectionString, logger);

        uploader.ServiceClient.Should().BeSameAs(downloader.ServiceClient);
    }

    [Test]
    public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
    {
        var act = () => new BlobDownloader(null, logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("azureStorageConnectionString");
    }

    [Test]
    public void Constructor_WithMalformedConnectionString_ThrowsOnEachCall()
    {
        var connectionString = fixture.Create<string>();

        var act = () => new BlobDownloader(connectionString, logger);

        act.Should().Throw<FormatException>();
        act.Should().Throw<FormatException>();
    }

    /// <summary>
    /// The client parses the connection string when it is made, so the account name and key are made up but well-formed.
    /// </summary>
    private string ConnectionString() =>
        $"DefaultEndpointsProtocol=https;AccountName={fixture.Create<string>()};" +
        $"AccountKey={Convert.ToBase64String(fixture.CreateMany<byte>(64).ToArray())};EndpointSuffix=core.windows.net";
}
