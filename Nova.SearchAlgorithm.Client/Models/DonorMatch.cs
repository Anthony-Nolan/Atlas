using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Client.Models
{
    [Flags]
    public enum MatchDescription
    {
        // TODO:NOVA-924 define how we describe the way this donor matched; the below is incorrect
        A = 0,
        B = 1,
        C = 2,
        DRB1 = 4,
        DQB1 = 8
    }

    public class DonorMatch
    {
        public MatchDescription MatchDescription { get; set; }
        public Donor Donor { get; set; }
    }
}