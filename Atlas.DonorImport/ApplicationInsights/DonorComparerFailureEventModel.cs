using System;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Models;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class DonorComparerFailureEventModel : EventModel
    {
        public DonorComparerFailureEventModel(DonorImportFile file, Exception exception) : base("Donor Comparison Failed")
        {
            Level = LogLevel.Warn;
            Properties.Add(nameof(file.FileLocation), file.FileLocation);
            Properties.Add("Exception", exception.ToString());
        }
    }
}
