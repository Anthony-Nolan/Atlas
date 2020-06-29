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
        /// </summary>
        public string NmdpCode { get; set; }
        
        /// <summary>
        /// The corresponding serology for the given allele.
        /// </summary>
        public string Serology { get; set; }
        
        /// <summary>
        /// The corresponding p-group for the given allele.
        /// </summary>
        public string PGroup { get; set; }
        
        /// <summary>
        /// The corresponding g-group for the given allele.
        /// </summary>
        public string GGroup { get; set; }
    }
}