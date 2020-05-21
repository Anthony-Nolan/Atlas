using System.IO;
using System.Threading.Tasks;

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Atlas.DonorImport.Services
{
    public interface IDonorFileImporter
    {
        Task ImportDonorFile(Stream fileStream);
    }

    internal class DonorFileImporter : IDonorFileImporter
    {
        private readonly IDonorImportFileParser fileParser;
        private readonly IDonorRecordChangeApplier donorRecordChangeApplier;

        public DonorFileImporter(IDonorImportFileParser fileParser, IDonorRecordChangeApplier donorRecordChangeApplier)
        {
            this.fileParser = fileParser;
            this.donorRecordChangeApplier = donorRecordChangeApplier;
        }

        public async Task ImportDonorFile(Stream fileStream)
        {
            var donorUpdates = fileParser.LazilyParseDonorUpdates(fileStream);
            foreach (var donorUpdate in donorUpdates)
            {
                await donorRecordChangeApplier.ApplyDonorOperationBatch(new[] {donorUpdate});
            }
        }
    }
}