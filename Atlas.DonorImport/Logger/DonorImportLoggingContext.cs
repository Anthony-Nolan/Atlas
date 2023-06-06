using Atlas.Common.ApplicationInsights;
using System.Collections.Generic;

namespace Atlas.DonorImport.Logger
{
    public class DonorImportLoggingContext : LoggingContext
    {
        public string Filename { get; set; }

        public override Dictionary<string, string> PropertiesToLog()
            => new Dictionary<string, string>
            {
                {nameof(Filename), Filename}
            };
    }
}
