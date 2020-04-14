namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla
{
    public class AlleleTestData
    {
        /// <summary>
        /// A known TGS typed single allele
        /// </summary>
        public string AlleleName { get; set; }
        
        /// <summary>
        /// A manually selected corresponding NMDP code.
        /// Used in donor hla data
        /// </summary>
        public string NmdpCode { get; set; }
        
        /// <summary>
        /// The corresponding serology for the given allele.
        /// Used in donor hla data
        /// </summary>
        public string Serology { get; set; }
        
        /// <summary>
        /// The corresponding p-group for the given allele.
        /// Used only for identification of p-group level matches
        /// </summary>
        public string PGroup { get; set; }
        
        /// <summary>
        /// The corresponding g-group for the given allele.
        /// Used only for identification of g-group level matches
        /// </summary>
        public string GGroup { get; set; }
    }
}