using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class Donor
    {
        public string DonorId { get; set; }
        // TODO:NOVA-924 make donor type a strongly typed Enum
        public string DonorType { get; set; }
        public RegistryCode Registry { get; set; }

        // TODO: NOVA-924 Do we need to include the (original) phenotype,
        // or can the search client retrieve that from the original registry?   
        // TODO: NOVA-930 add fields for sorting such as birth date, gender, TNC final count.
    }
}