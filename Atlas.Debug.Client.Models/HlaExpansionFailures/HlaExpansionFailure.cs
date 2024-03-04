using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Debug.Client.Models.HlaExpansionFailures
{
    public class HlaExpansionFailure
    {
        public string InvalidHLA { set; get; }
        public string ExceptionType { set; get; }
        public IEnumerable<string> ExternalDonorCodes { set; get; }
        public long DonorCount { set; get; }
    }
}
