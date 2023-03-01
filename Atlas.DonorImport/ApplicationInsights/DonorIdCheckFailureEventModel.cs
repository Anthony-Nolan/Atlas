using System;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Models;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class DonorIdCheckFailureEventModel : EventModel
    {
        public DonorIdCheckFailureEventModel(DonorIdCheckFile file, Exception exception) : base("Donor Id Check Failed")
        {
            Level = LogLevel.Warn;
            Properties.Add(nameof(file.FileLocation), file.FileLocation);
            Properties.Add("Exception", exception.ToString());
        }
    }
}
