using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Exceptions;

namespace Nova.SearchAlgorithm.Data.Entity
{
    public class MatchingHla
    {
        public int Id { get; set; }
        public int DonorId { get; set; }
        public int TypePosition { get; set; }
        public int LocusCode { get; set; }
        public PGroupName PGroup { get; set; }
    }
}