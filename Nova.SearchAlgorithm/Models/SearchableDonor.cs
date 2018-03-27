using Nova.SearchAlgorithm.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Models
{
    // TODO:NOVA-919 rename as appropriate
    public class SearchableDonor
    {
        public string DonorId { get; set; }
        public string DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }


        // TODO: NOVA-930 add fields for sorting such as birth date, gender, TNC final count.

        public Donor ToApiDonor()
        {
            return new Donor
            {
                DonorId = DonorId,
                DonorType = DonorType,
                Registry = RegistryCode
            };
        }
    }
}