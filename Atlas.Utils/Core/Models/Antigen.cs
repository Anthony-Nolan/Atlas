namespace Atlas.Utils.Core.Models
{
    public class Antigen
    {
        public int? Id { get; set; } // This is the Antigen_Id in the DR_ANTIGENS table in oracle
        public LocusType Locus { get; set; }
        public string HlaName { get; set; }
        public string NmdpString { get; set; }
    }
}