using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using MoreLinq;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    internal interface IDonorIdChecker
    {
        Task CheckDonorIdsFromFile(DonorImportFile file);
    }

    internal class DonorIdChecker : IDonorIdChecker
    {
        private const int BatchSize = 10000;
        private readonly IDonorIdCheckerFileParser fileParser;
        private readonly IDonorReader donorReader;

        public DonorIdChecker(IDonorIdCheckerFileParser fileParser, IDonorReader donorReader)
        {
            this.fileParser = fileParser;
            this.donorReader = donorReader;
        }

        public async Task CheckDonorIdsFromFile(DonorImportFile file)
        {
            var lazyFile = fileParser.PrepareToLazilyParsingDonorIdFile(file.Contents);

            try
            {
                foreach (var donorIdsBatch in lazyFile.ReadLazyDonorIds().Batch(BatchSize))
                {
                    var donors = await donorReader.GetDonorsByExternalDonorCodes(donorIdsBatch);
                    var donorIdCheckResults = new DonorIdCheckerResults
                    {
                        Results = donorIdsBatch.Select(id => new DonorIdCheckerResult
                        {
                            RecordId = id,
                            IsPresentInDonorStore = donors.ContainsKey(id)
                        })
                    };
                }
            }
            catch
            {

            }
            
            throw new System.NotImplementedException();
        }
    }
}
