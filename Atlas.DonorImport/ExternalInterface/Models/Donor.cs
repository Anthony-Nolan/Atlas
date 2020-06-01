using DatabaseDonor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.ExternalInterface.Models
{
    public class Donor : DatabaseDonor 
    {
        
    }

    public static class DatabaseDonorExtensions
    {
        public static Donor ToPublicDonor(this DatabaseDonor donor)
        {
            return donor as Donor;
        }
    }
}