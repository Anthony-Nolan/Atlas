using System;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class DonorImportFailureEventModel : EventModel
    {
        public DonorImportFailureEventModel(DonorImportFile file, Exception exception, int importedDonorCount, LazilyParsingDonorFile lazyFile) : base("Donor Import Failed")
        {
            Level = LogLevel.Warn;
            Properties.Add(nameof(file.FileLocation), file.FileLocation);
            Properties.Add("Exception", exception.ToString());
            Properties.Add("ImportedDonorCount", importedDonorCount.ToString());
            Properties.Add("ParsedDonorCount", lazyFile?.ParsedDonorCount.ToString());
            Properties.Add("LastDonorCodeParsed", lazyFile?.LastSuccessfullyParsedDonorCode);
        }
    }
}
