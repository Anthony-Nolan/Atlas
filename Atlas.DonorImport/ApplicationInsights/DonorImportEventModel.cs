using Atlas.Common.ApplicationInsights;
using System;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class DonorImportEventModel : EventModel
    {
        public DonorImportEventModel(DonorImportFile file, string exception, int importedDonorCount, LazilyParsingDonorFile lazyFile) : base("Donor Import Failed")
        {
            var message = @$"Donor Import Failed: {importedDonorCount} Donors were successfully imported prior to this error and have already been stored in the Database.
Any remaining donors in the file have not been stored. The first {lazyFile?.ParsedDonorCount} Donors were able to be parsed from the file.
The last Donor to be *successfully* parsed had DonorCode '{lazyFile?.LastSuccessfullyParsedDonorCode}'. Manual investigation is recommended;";

            Level = LogLevel.Warn;
            Properties.Add(nameof(file.FileLocation), file.FileLocation);
            Properties.Add("Message", message);
            Properties.Add("Exception", exception);
            Properties.Add(nameof(file.FileLocation), file.FileLocation);
        }
    }
}
