using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq.Extensions;

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Atlas.DonorImport.Services
{
    public interface IDonorFileImporter
    {
        Task ImportDonorFile(Stream fileStream);
    }

    internal class DonorFileImporter : IDonorFileImporter
    {
        private const int BatchSize = 10000;
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
            foreach (var donorUpdateBatch in donorUpdates.Batch(BatchSize))
            {
                await donorRecordChangeApplier.ApplyDonorRecordChangeBatch(donorUpdateBatch.ToList());
            }
        }
    }
}