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
        private readonly IDonorOperationApplier donorOperationApplier;

        public DonorFileImporter(IDonorImportFileParser fileParser, IDonorOperationApplier donorOperationApplier)
        {
            this.fileParser = fileParser;
            this.donorOperationApplier = donorOperationApplier;
        }

        public async Task ImportDonorFile(Stream fileStream)
        {
            var donorUpdates = fileParser.LazilyParseDonorUpdates(fileStream);
            await foreach (var donorUpdate in donorUpdates)
            {
                await donorOperationApplier.ApplyDonorOperationBatch(new[] {donorUpdate});
            }
        }
    }
}