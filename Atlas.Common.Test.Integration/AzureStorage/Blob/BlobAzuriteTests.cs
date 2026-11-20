using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using AwesomeAssertions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.Extensions;
using NUnit.Framework;

namespace Atlas.Common.Test.Integration.AzureStorage.Blob
{
    /// <summary>
    /// Runs BlobUploader and BlobDownloader against Azurite, the Azure Storage emulator. The pipeline job that starts Azurite
    /// sets RUN_AZURITE_TESTS; without that variable the fixture ignores itself.
    /// Each test uses a new container, so the per-process cache of container checks cannot connect one test to another.
    /// </summary>
    [TestFixture]
    public class BlobAzuriteTests
    {
        private const string AzuriteVariable = "RUN_AZURITE_TESTS";
        private const string ConnectionString = "UseDevelopmentStorage=true";
        private const string Folder = "results";

        // More than 9 files, so that blob listing (name) order differs from number order for file names without zero padding:
        // "1.json", "10.json", "11.json", "2.json", ...
        private const int UnpaddedFileCount = 11;

        public enum FolderRead
        {
            AllFiles,
            FileByFile,
        }

        public enum MissingBlobRead
        {
            Download,
            OpenRead,
            DownloadMultipleBlobs,
        }

        private IAtlasLogger logger;
        private string containerName;
        private BlobContainerClient container;
        private BlobUploader uploader;
        private BlobDownloader downloader;

        [SetUp]
        public void SetUp()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AzuriteVariable)))
            {
                Assert.Ignore($"Set {AzuriteVariable} and start Azurite. See README_DevelopmentStartUpGuide.md.");
            }

            logger = Substitute.For<IAtlasLogger>();
            containerName = "blob-" + Guid.NewGuid().ToString("N");
            container = new BlobContainerClient(ConnectionString, containerName);
            uploader = new BlobUploader(ConnectionString, logger);
            downloader = new BlobDownloader(ConnectionString, logger);
        }

        [TearDown]
        public async Task TearDown()
        {
            if (container != null)
            {
                await container.DeleteIfExistsAsync();
                container = null;
            }
        }

        [Test]
        public async Task Upload_ToMissingContainer_CreatesContainerAndStoresBlob()
        {
            (await container.ExistsAsync()).Value.Should().BeFalse("each test starts with a container that does not exist yet");

            await uploader.Upload(containerName, "first.json", Contents());

            (await container.ExistsAsync()).Value.Should().BeTrue();
            (await BlobNames()).Should().Equal("first.json");
        }

        [Test]
        public async Task Upload_SecondTimeToSameContainer_StoresBothBlobs()
        {
            await uploader.Upload(containerName, "first.json", Contents());
            await uploader.Upload(containerName, "second.json", Contents());

            (await BlobNames()).Should().Equal("first.json", "second.json");
        }

        [Test]
        public async Task Download_UploadedBlob_ReturnsSameContents()
        {
            var uploaded = Contents();
            await uploader.Upload(containerName, "results.json", uploaded);

            var downloaded = await downloader.Download<BlobContents>(containerName, "results.json");

            downloaded.Should().BeEquivalentTo(uploaded);
        }

        /// <summary>
        /// The race the catch in CreateContainerIfMissing is for: another process makes the container after the existence check.
        /// Two uploads in one process share one check, so the race is made here: the container exists, but the check says it does not.
        /// Only the check is faked; the create request goes to Azurite, which answers 409. If the error code in that answer
        /// were not the one the catch accepts, the upload would throw.
        /// </summary>
        [Test]
        public async Task Upload_WhenContainerIsCreatedAfterExistenceCheck_StoresBlob()
        {
            await container.CreateAsync();
            var containerWithStaleCheck = Substitute.ForPartsOf<BlobContainerClient>(ConnectionString, containerName);
            containerWithStaleCheck.Configure().ExistsAsync(Arg.Any<CancellationToken>())
                .Returns(Response.FromValue(false, Substitute.For<Response>()));
            var serviceClient = Substitute.For<BlobServiceClient>();
            serviceClient.GetBlobContainerClient(containerName).Returns(containerWithStaleCheck);

            await new BlobUploader(serviceClient, logger).Upload(containerName, "first.json", Contents());

            await containerWithStaleCheck.Received(1).CreateAsync(
                Arg.Any<PublicAccessType>(),
                Arg.Any<IDictionary<string, string>>(),
                Arg.Any<BlobContainerEncryptionScopeOptions>(),
                Arg.Any<CancellationToken>());
            (await BlobNames()).Should().Equal("first.json");
        }

        [Test]
        public async Task ChunkAndUpload_WritesNumberedFilesThatDownloadInOriginalOrder()
        {
            const int batchSize = 2;
            var items = Enumerable.Range(1, 5).Select(Contents).ToList();

            await uploader.ChunkAndUpload(items, batchSize, containerName, Folder);

            (await BlobNames()).Should().Equal(
                BatchedResultsFileNames.FileName(Folder, 1),
                BatchedResultsFileNames.FileName(Folder, 2),
                BatchedResultsFileNames.FileName(Folder, 3));
            var downloaded = await downloader.DownloadFolderContents<BlobContents>(containerName, Folder);
            downloaded.Should().BeEquivalentTo(items, options => options.WithStrictOrdering());
        }

        /// <summary>
        /// Older results folders have file names without zero padding, which Azure lists in name order, not number order.
        /// The order of the files is the ranking order of the results, so both reads must return them in number order.
        /// A folder whose name starts with the same text must not be read with it.
        /// </summary>
        [TestCase(FolderRead.AllFiles)]
        [TestCase(FolderRead.FileByFile)]
        public async Task DownloadFolder_WithUnpaddedFileNames_ReturnsFilesInNumberOrder(FolderRead read)
        {
            var files = Enumerable.Range(1, UnpaddedFileCount)
                .ToDictionary(number => $"{Folder}/{number}.json", number => new List<BlobContents> { Contents(number) });
            files.Add($"{Folder}-other/1.json", new List<BlobContents> { Contents(0) });
            await uploader.UploadMultiple(containerName, files);

            var downloaded = await DownloadFolder(read);

            downloaded.Select(c => c.DonorCount).Should().Equal(Enumerable.Range(1, UnpaddedFileCount));
        }

        [Test]
        public async Task DownloadFolderContentsFileByFile_ReturnsOneCollectionPerFile()
        {
            const int batchSize = 2;
            var items = Enumerable.Range(1, 5).Select(Contents).ToList();
            await uploader.ChunkAndUpload(items, batchSize, containerName, Folder);

            var files = new List<List<BlobContents>>();
            await foreach (var file in downloader.DownloadFolderContentsFileByFile<BlobContents>(containerName, Folder))
            {
                files.Add(file.ToList());
            }

            files.Select(file => file.Select(c => c.DonorCount)).Should().BeEquivalentTo(
                new[] { new[] { 1, 2 }, new[] { 3, 4 }, new[] { 5 } },
                options => options.WithStrictOrdering());
        }

        [Test]
        public async Task DownloadMultipleBlobs_ReturnsEachBlobUnderItsId()
        {
            // Fewer than the number of blobs, so the download takes more than one batch.
            const int batchSize = 2;
            var uploaded = Enumerable.Range(1, 5).ToDictionary(id => $"result-{id}.json", Contents);
            await uploader.UploadMultiple(containerName, uploaded);
            var locations = Enumerable.Range(1, 5).ToDictionary(id => id, id => $"result-{id}.json");

            var downloaded = await downloader.DownloadMultipleBlobs<BlobContents>(containerName, locations, batchSize);

            downloaded.Should().BeEquivalentTo(locations.ToDictionary(location => location.Key, location => uploaded[location.Value]));
        }

        [Test]
        public async Task OpenRead_UploadedBlob_StreamsSameContents()
        {
            var uploaded = Contents();
            await uploader.Upload(containerName, "results.json", uploaded);

            await using var stream = await downloader.OpenRead(containerName, "results.json");
            using var reader = new StreamReader(stream);
            var downloaded = JsonConvert.DeserializeObject<BlobContents>(await reader.ReadToEndAsync());

            downloaded.Should().BeEquivalentTo(uploaded);
        }

        /// <summary>
        /// The downloader turns the "BlobNotFound" error code into a BlobNotFoundException. A unit test cannot show that a real
        /// response has that code, so this test reads a blob that is not there from a container that is.
        /// </summary>
        [TestCase(MissingBlobRead.Download)]
        [TestCase(MissingBlobRead.OpenRead)]
        [TestCase(MissingBlobRead.DownloadMultipleBlobs)]
        public async Task Read_MissingBlob_ThrowsBlobNotFoundException(MissingBlobRead read)
        {
            await uploader.Upload(containerName, "present.json", Contents());

            var act = () => ReadMissingBlob(read, "missing.json");

            await act.Should().ThrowAsync<BlobNotFoundException>();
        }

        private async Task<List<BlobContents>> DownloadFolder(FolderRead read)
        {
            switch (read)
            {
                case FolderRead.AllFiles:
                    return (await downloader.DownloadFolderContents<BlobContents>(containerName, Folder)).ToList();
                case FolderRead.FileByFile:
                    var contents = new List<BlobContents>();
                    await foreach (var file in downloader.DownloadFolderContentsFileByFile<BlobContents>(containerName, Folder))
                    {
                        contents.AddRange(file);
                    }

                    return contents;
                default:
                    throw new ArgumentOutOfRangeException(nameof(read), read, null);
            }
        }

        private async Task ReadMissingBlob(MissingBlobRead read, string filename)
        {
            switch (read)
            {
                case MissingBlobRead.Download:
                    await downloader.Download<BlobContents>(containerName, filename);
                    break;
                case MissingBlobRead.OpenRead:
                    await using (await downloader.OpenRead(containerName, filename))
                    {
                    }

                    break;
                case MissingBlobRead.DownloadMultipleBlobs:
                    await downloader.DownloadMultipleBlobs<BlobContents>(containerName, new Dictionary<int, string> { { 1, filename } }, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(read), read, null);
            }
        }

        /// <summary>
        /// Read with the SDK, not with BlobDownloader, so that the upload tests do not depend on the class under test.
        /// </summary>
        private async Task<List<string>> BlobNames()
        {
            var names = new List<string>();
            await foreach (var blob in container.GetBlobsAsync())
            {
                names.Add(blob.Name);
            }

            return names;
        }

        private static BlobContents Contents() => Contents(3);

        /// <param name="donorCount">Tells the contents apart where a test checks their order.</param>
        private static BlobContents Contents(int donorCount) => new()
        {
            SearchIdentifier = Guid.NewGuid().ToString(),
            DonorCount = donorCount,
            DonorCodes = Enumerable.Range(1, donorCount).Select(number => $"donor-{number}").ToList(),
            CompletedAt = new DateTimeOffset(2026, 11, 19, 10, 30, 0, TimeSpan.Zero),
        };

        public class BlobContents
        {
            public string SearchIdentifier { get; set; }
            public int DonorCount { get; set; }
            public List<string> DonorCodes { get; set; }
            public DateTimeOffset CompletedAt { get; set; }
        }
    }
}
