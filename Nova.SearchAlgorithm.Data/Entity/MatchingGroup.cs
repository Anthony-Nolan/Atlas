namespace Nova.SearchAlgorithm.Data.Entity
{
    public abstract class MatchingGroup
    {
        public int Id { get; set; }
        public int DonorId { get; set; }
        public int TypePosition { get; set; }
        public PGroupName PGroup { get; set; }
    }
}