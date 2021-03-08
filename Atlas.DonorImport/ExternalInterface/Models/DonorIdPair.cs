namespace Atlas.DonorImport.ExternalInterface.Models
{
    /// <summary>
    /// Throughout Atlas, two forms of donor identifier are used - an external one, and an internal one.
    /// This class exists as a more readable way of using both together than a tuple.
    /// </summary>
    public class DonorIdPair
    {
        public string ExternalDonorCode { get; set; }
        public int AtlasId { get; set; }
    }
}