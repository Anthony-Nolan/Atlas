using Atlas.Utils.Core.Models;

namespace Atlas.HLAService.Client.Models
{
    public class AntigenLookupModel
    {
        public LocusType Locus { get; set; }
        public string HlaName { get; set; }
        public string NmdpString { get; set; }
    }
}