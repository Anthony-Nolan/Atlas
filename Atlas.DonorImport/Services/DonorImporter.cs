using System.IO;

namespace Atlas.DonorImport.Services
{
    public interface IDonorImporter
    {
        void ImportDonorFile(Stream fileStream);
    }

    internal class DonorImporter : IDonorImporter
    {
        private readonly IDonorImportFileParser fileParser;

        public DonorImporter(IDonorImportFileParser fileParser)
        {
            this.fileParser = fileParser;
        }
        
        public void ImportDonorFile(Stream fileStream)
        {
            var donorUpdates = fileParser.LazilyParseDonorUpdates(fileStream);
        }
    }
}