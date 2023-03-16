using System;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Models;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class DonorInfoCheckerFailureEventModel : EventModel
    {
        public DonorInfoCheckerFailureEventModel(DonorImportFile file, Exception exception) : base("Donor Info Check Failed")
        {
            Level = LogLevel.Warn;
            Properties.Add(nameof(file.FileLocation), file.FileLocation);
            Properties.Add("Exception", exception.ToString());
        }
    }
}
