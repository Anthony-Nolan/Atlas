using System.IO;
using System.Text;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
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
}
