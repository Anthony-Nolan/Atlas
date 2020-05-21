using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Atlas.Common.Notifications;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    public class DonorFileImporterTests
    {
        private IDonorOperationApplier donorOperationApplier;
        private INotificationsClient notificationsClient;
        private const int BatchSize = 2;

        private IDonorFileImporter donorFileImporter;

        [SetUp]
        public void SetUp()
        {
            donorOperationApplier = Substitute.For<IDonorOperationApplier>();
            notificationsClient = Substitute.For<INotificationsClient>();

            donorFileImporter = new DonorFileImporter(donorOperationApplier, notificationsClient, BatchSize);
        }

        [Test]
        public void ImportDonorFile_ProcessesAllDonors()
        {
            const int fullBatches = 2;
            // Ensure multiple full batches, and one partial batch 
            var file = new DonorFile {donors = Enumerable.Range(0, BatchSize * fullBatches + 1).Select(i => new DonorUpdate())};
            var fileJson = JsonConvert.SerializeObject(file);
            var fileStream = new MemoryStream(Encoding.Default.GetBytes(fileJson));

            donorFileImporter.ImportDonorFile(fileStream, "fileName");

            donorOperationApplier.Received(fullBatches + 1).ApplyDonorOperationBatch(Arg.Any<UpdateMode>(), Arg.Any<IEnumerable<DonorUpdate>>());
        }

        private class DonorFile
        {
            // ReSharper disable once InconsistentNaming
            public IEnumerable<DonorUpdate> donors;
        }
    }
}