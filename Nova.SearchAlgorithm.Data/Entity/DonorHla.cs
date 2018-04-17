namespace Nova.SearchAlgorithm.Data.Entity
{
    public class DonorHla
    {
        public int Id { get; set; }
        public int DonorId { get; set; }
        public string Locus { get; set; }
        public int TypePosition { get; set; }
        public string HlaName { get; set; }
    }
}