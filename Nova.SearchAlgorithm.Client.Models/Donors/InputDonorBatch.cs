using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Client.Models.Donors
{
    public class InputDonorBatch
    {
        public IEnumerable<InputDonor> Donors { get; set; }
    }
}