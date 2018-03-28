using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Models
{
    public class RawDonor
    {
        public string DonorId { get; set; }

        // This might not match our own idea of registry codes, e.g. "AN" vs "ANBMT"
        public string RegistryCode { get; set; }
        
        // This might not match our own idea of types, e.g. "A" vs "Adult"
        public string DonorType { get; set; }

        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }
        public string DRB1_1 { get; set; }
        public string DRB1_2 { get; set; }
    }
}