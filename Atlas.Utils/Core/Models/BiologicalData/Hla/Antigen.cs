namespace Nova.Utils.Models
{
    public class Antigen
    {
        public int? Id { get; set; } // This is the Antigen_Id in the DR_ANTIGENS table in oracle
        public LocusType Locus { get; set; }
        /// <summary>
        /// A string representation of the antigen - following the international standard of nomenclature.
        /// One name can represent multiple alleles, in the case of uncertainty.
        /// e.g. 01:01/02 would represent uncertainty between the alleles 01:01 and 01:02
        ///
        /// This property will always be present.
        /// </summary>
        public string HlaName { get; set; }
        
        /// <summary>
        /// Otherwise referred to as "NMDP Codes"
        /// An NMDP code is a compressed representation of an uncertain HlaName.
        /// e.g. 14:AF represents 14:01/14:09
        ///
        /// This will only be populated in cases where the use of an NMDP code is relevant.
        /// In other cases (e.g. a single allele), this will be null.
        /// </summary>
        public string NmdpString { get; set; }
    }
}