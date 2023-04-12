using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Test.TestHelpers.Models.DonorIdCheck
{
    internal class SerializableDonorIdCheckerFileContent : DonorIdCheckerRequest
    {
        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }

    internal class SerializableDonorIdCheckerFileContentWithInvalidPropertyOrder
    {
        public string donPool { get; set; }
        public IEnumerable<string> donors { get; set; }
        public string donorType { get; set; }

        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }

    internal class SerializableDonorIdCheckerFileContentWithDonorPoolOnly
    {
        public string donPool { get; set; }

        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }

    internal class SerializableDonorIdCheckerFileContentWithDonorTypeOnly
    {
        public string donorType { get; set; }

        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }
}
