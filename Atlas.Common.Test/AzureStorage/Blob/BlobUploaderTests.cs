using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using AutoFixture;
using AwesomeAssertions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.Common.Test.AzureStorage.Blob;

[TestFixture]
public class BlobUploaderTests
{
    private const int ConflictStatus = 409;
    private const int ForbiddenStatus = 403;

    private Fixture fixture;
    private IAtlasLogger logger;
    private BlobClient blob;
    private string containerName;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        logger = Substitute.For<IAtlasLogger>();
        blob = Substitute.For<BlobClient>();
        containerName = fixture.Create<string>();
    }

    [Test]
    public async Task Upload_TwiceToSameContainer_ChecksExistenceOnce()
    {
        var container = Container(exists: true);
        var uploader = UploaderFor(container);

        await Upload(uploader);
        await Upload(uploader);

        await container.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await BlobUploads(blob.Received(2));
    }

    [Test]
    public async Task Upload_TwiceAtSameTimeToSameContainer_ChecksExistenceOnce()
    {
        var container = Container(exists: true);
        var existence = new TaskCompletionSource<Response<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        container.ExistsAsync(Arg.Any<CancellationToken>()).Returns(existence.Task);
        var uploader = UploaderFor(container);

        var first = Upload(uploader);
        var second = Upload(uploader);
        existence.SetResult(Exists(true));
        await Task.WhenAll(first, second);

        await container.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await BlobUploads(blob.Received(2));
    }

    [Test]
    public async Task Upload_ToSameContainerNameOnTwoAccounts_ChecksEachAccountOnce()
    {
        var containerOnFirstAccount = Container(exists: true);
        var containerOnSecondAccount = Container(exists: true);
        var firstUploader = UploaderFor(containerOnFirstAccount);
        var secondUploader = UploaderFor(containerOnSecondAccount);

        await Upload(firstUploader);
        await Upload(secondUploader);
        await Upload(firstUploader);
        await Upload(secondUploader);

        await containerOnFirstAccount.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await containerOnSecondAccount.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Upload_ToMissingContainer_CreatesContainerOnce()
    {
        var container = Container(exists: false);
        var uploader = UploaderFor(container);

        await Upload(uploader);
        await Upload(uploader);

        await container.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await ContainerCreation(container.Received(1));
        await BlobUploads(blob.Received(2));
    }

    [Test]
    public async Task Upload_WhenCreateFailsWithContainerAlreadyExists_UploadsBlobAndKeepsCheck()
    {
        var container = Container(exists: false);
        ContainerCreation(container).ThrowsAsync(Failure(ConflictStatus, BlobErrorCode.ContainerAlreadyExists));
        var uploader = UploaderFor(container);

        var act = async () =>
        {
            await Upload(uploader);
            await Upload(uploader);
        };

        await act.Should().NotThrowAsync();
        await container.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await BlobUploads(blob.Received(2));
    }

    [Test]
    public async Task Upload_AfterFailedExistenceCheck_ChecksAgain()
    {
        var container = Container(exists: true);
        container.ExistsAsync(Arg.Any<CancellationToken>()).Returns(
            Task.FromException<Response<bool>>(new RequestFailedException(fixture.Create<string>())),
            Task.FromResult(Exists(true)));
        var uploader = UploaderFor(container);

        var firstUpload = () => Upload(uploader);

        await firstUpload.Should().ThrowAsync<RequestFailedException>();
        await Upload(uploader);
        await container.Received(2).ExistsAsync(Arg.Any<CancellationToken>());
        await BlobUploads(blob.Received(1));
    }

    [Test]
    public async Task Upload_WhenCreateFailsWithOtherError_Throws()
    {
        var container = Container(exists: false);
        var failure = Failure(ForbiddenStatus, BlobErrorCode.AuthorizationFailure);
        ContainerCreation(container).ThrowsAsync(failure);
        var uploader = UploaderFor(container);

        var act = () => Upload(uploader);

        (await act.Should().ThrowAsync<RequestFailedException>()).Which.Should().BeSameAs(failure);
    }

    /// <summary>
    /// Each container has its own URI, so the static cache of container checks cannot connect one test to another.
    /// </summary>
    private BlobContainerClient Container(bool exists)
    {
        var container = Substitute.For<BlobContainerClient>();
        container.Uri.Returns(fixture.Create<Uri>());
        container.ExistsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Exists(exists)));
        container.GetBlobClient(Arg.Any<string>()).Returns(blob);
        return container;
    }

    private BlobUploader UploaderFor(BlobContainerClient container)
    {
        var serviceClient = Substitute.For<BlobServiceClient>();
        serviceClient.GetBlobContainerClient(containerName).Returns(container);
        return new BlobUploader(serviceClient, logger);
    }

    private Task Upload(BlobUploader uploader) => uploader.Upload(containerName, fixture.Create<string>(), fixture.Create<string>());

    private RequestFailedException Failure(int status, BlobErrorCode errorCode) =>
        new(status, fixture.Create<string>(), errorCode.ToString(), null);

    private static Response<bool> Exists(bool exists) => Response.FromValue(exists, Substitute.For<Response>());

    private static Task<Response<BlobContainerInfo>> ContainerCreation(BlobContainerClient container) =>
        container.CreateAsync(
            Arg.Any<PublicAccessType>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<BlobContainerEncryptionScopeOptions>(),
            Arg.Any<CancellationToken>());

    private static Task<Response<BlobContentInfo>> BlobUploads(BlobClient blobClient) =>
        blobClient.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>());
}
