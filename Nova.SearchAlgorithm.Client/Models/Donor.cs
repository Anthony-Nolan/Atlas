using System;
using Nova.Utils.Models;

namespace Nova.SearchAlgorithm.Client.Models
{
    // TODO add to utils to share with SearchService?
    public class Donor
    {
        public string Id { get; set; }
        public string HomeRegistryDonorId { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; }
        public string Weight { get; set; }
        public string BloodType { get; set; }
        public string RhD { get; set; }
        public CmvAntibodyType Cmv { get; set; }
        public DateTime? DateCmvTested { get; set; }
        public string Ethnicity { get; set; }
        public string DonorStatus { get; set; }
        public string UnavailabilityReason { get; set; }
        public DateTime? UnavailableToDate { get; set; }
        public string ReservedForPatientId { get; set; }
        public DateTime? LastVerified { get; set; }
        public Hla Hla { get; set; } = new Hla();
    }
}
