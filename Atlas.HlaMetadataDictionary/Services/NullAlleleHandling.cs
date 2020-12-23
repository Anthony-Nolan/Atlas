using System.Linq;

namespace Atlas.HlaMetadataDictionary.Services
{
    internal static class NullAlleleHandling
    {
        private const string NullAlleleDivider = "[NULL-AS]";
        
        /// <summary>
        /// Null-expressing alleles are considered matches based on the expressing allele at that locus.
        ///
        /// To achieve this, when converting to p-group data, we should consider the null allele to have the p-groups of the expressing allele.
        ///
        /// However, we need this list of p-groups not to be strongly linked to the null allele itself - as it may have a different list of
        /// p-groups when paired with a different expressing allele.
        ///
        /// We also need to be able to identify the original null allele at this position, for use in scoring, which follows different rules
        /// for null-expressing alleles.
        /// </summary>
        ///
        /// <returns>
        /// A composite allele name, unique for the combination of null and expressing allele
        /// - from which the original null allele name can be extracted.
        /// </returns>
        public static string CombineAlleleNames(string nullAlleleName, string expressingAlleleName)
            => $"{nullAlleleName}{NullAlleleDivider}{expressingAlleleName}";

        /// <summary>
        /// If provided with a combined allele name as created in <see cref="CombineAlleleNames" />, will return the original null allele.
        /// If provided with a non-combined allele, returns the input allele. 
        /// </summary>
        /// <param name="combinedAlleleName"></param>
        /// <returns></returns>
        public static string GetOriginalAlleleFromCombinedName(string combinedAlleleName) => combinedAlleleName.Split(NullAlleleDivider).First();
    }
}