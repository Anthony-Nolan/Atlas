using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    public class DonorImportFileParserTests
    {
        private IDonorImportFileParser donorImportFileParser;

        [SetUp]
        public void SetUp()
        {
            donorImportFileParser = new DonorImportFileParser();
        }

        [Test]
        public void ImportDonorFile_ProcessesAllDonors()
        {
            const int donorCount = 100;
            var file = new DonorFile {updateMode = UpdateMode.Differential, donors = Enumerable.Range(0, donorCount).Select(i => new DonorUpdate())};
            var fileJson = JsonConvert.SerializeObject(file);
            var fileStream = new MemoryStream(Encoding.Default.GetBytes(fileJson));

            var donors = donorImportFileParser.LazilyParseDonorUpdates(fileStream).ToList();

            donors.Count().Should().Be(donorCount);
        }

        private class DonorFile
        {
            // ReSharper disable once InconsistentNaming
            public UpdateMode updateMode { get; set; }

            // ReSharper disable once InconsistentNaming
            public IEnumerable<DonorUpdate> donors { get; set; }
        }
    }
}