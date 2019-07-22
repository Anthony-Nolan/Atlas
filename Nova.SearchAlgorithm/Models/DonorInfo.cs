namespace Nova.SearchAlgorithm.Models
{
    /// <summary>
    /// Raw data for donor that will be imported into the search algorithm.
    /// </summary>
    public class DonorInfo
    {
        public int DonorId { get; set; }
        public string DonorType { get; set; }
        public string RegistryCode { get; set; }
        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DPB1_1 { get; set; }
        public string DPB1_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }
        public string DRB1_1 { get; set; }
        public string DRB1_2 { get; set; }
    }
}
