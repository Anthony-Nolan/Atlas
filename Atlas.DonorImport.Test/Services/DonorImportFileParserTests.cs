using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    public class DonorImportFileParserTests
    {
        private const int BatchSize = 2;

        private IDonorImportFileParser donorImportFileParser;

        [SetUp]
        public void SetUp()
        {
            donorImportFileParser = new DonorImportFileParser();
        }

        [Test]
        public async Task ImportDonorFile_ProcessesAllDonors()
        {
            const int fullBatches = 2;
            // Ensure multiple full batches, and one partial batch 
            const int donorCount = BatchSize * fullBatches + 1;
            var file = new DonorFile {donors = Enumerable.Range(0, donorCount).Select(i => new DonorUpdate())};
            var fileJson = JsonConvert.SerializeObject(file);
            var fileStream = new MemoryStream(Encoding.Default.GetBytes(fileJson));

            var donorBatches = await donorImportFileParser.LazilyParseDonorUpdates(fileStream).ToListAsync();

            donorBatches.SelectMany(b => b).Count().Should().Be(donorCount);
        }

        private class DonorFile
        {
            // ReSharper disable once InconsistentNaming
            public IEnumerable<DonorUpdate> donors;
        }
    }
}